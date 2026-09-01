using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddPayrollPaymentsPayslipsAndSettlementClosure is explicitly approved and applied to Railway Development.")]
[Category("Phase6CPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlPayrollSettlementBehavioralAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [Test]
    public async Task PaymentAppendOnlyUpdateAndDeleteAreRejected()
    {
        var scenario = await CreateScenarioAsync(); var payment = await InsertCashPaymentAsync(scenario, 10m, "immutable-payment");
        await AssertRejectedAsync("UPDATE payroll.\"PayrollPayments\" SET \"AmountUsd\" = 11 WHERE \"Id\" = @id", ("id", payment));
        await AssertRejectedAsync("DELETE FROM payroll.\"PayrollPayments\" WHERE \"Id\" = @id", ("id", payment));
    }

    [Test]
    public async Task AcknowledgementImmutabilityIsEnforced()
    {
        var scenario = await CreateScenarioAsync(); var payment = await InsertCashPaymentAsync(scenario, 10m, "ack-payment"); var acknowledgement = await InsertAcknowledgementAsync(scenario, payment, "ack");
        await AssertRejectedAsync("UPDATE payroll.\"PaymentAcknowledgements\" SET \"Status\" = 'Declined' WHERE \"Id\" = @id", ("id", acknowledgement));
        await AssertRejectedAsync("DELETE FROM payroll.\"PaymentAcknowledgements\" WHERE \"Id\" = @id", ("id", acknowledgement));
    }

    [Test]
    public async Task ReversalImmutabilityAndRemainingAmountAreEnforced()
    {
        var scenario = await CreateScenarioAsync(); var payment = await InsertCashPaymentAsync(scenario, 20m, "reversal-payment"); var reversal = await InsertReversalAsync(scenario, payment, 10m, "reversal-one");
        await AssertRejectedAsync("UPDATE payroll.\"PayrollPaymentReversals\" SET \"Reason\" = 'tamper' WHERE \"Id\" = @id", ("id", reversal));
        await AssertRejectedAsync("DELETE FROM payroll.\"PayrollPaymentReversals\" WHERE \"Id\" = @id", ("id", reversal));
        await AssertRejectedAsync(ReversalSql, ScenarioParameters(scenario, payment, 10.01m, "reversal-over"));
    }

    [Test]
    public async Task SettlementCloseImmutabilityAndClosureIntegrityAreEnforced()
    {
        var unpaid = await CreateScenarioAsync(); await AssertRejectedAsync(ClosureSql, ClosureParameters(unpaid, "unpaid-close"));
        var scenario = await CreateScenarioAsync(); var payment = await InsertCashPaymentAsync(scenario, 100m, "settled-payment"); await InsertAcknowledgementAsync(scenario, payment, "settled-ack"); var closure = await InsertClosureAsync(scenario, "settled-close");
        await AssertRejectedAsync("UPDATE payroll.\"PayrollSettlementClosures\" SET \"WorkerCount\" = 2 WHERE \"Id\" = @id", ("id", closure));
        await AssertRejectedAsync("DELETE FROM payroll.\"PayrollSettlementClosures\" WHERE \"Id\" = @id", ("id", closure));
        await AssertRejectedAsync(PaymentSql, PaymentParameters(scenario, Guid.NewGuid(), 1m, "after-close"));
    }

    [Test]
    public async Task PaymentIdempotencyAndMobileReferenceUniquenessAreEnforced()
    {
        var scenario = await CreateScenarioAsync(); await InsertCashPaymentAsync(scenario, 10m, "same-key");
        await AssertRejectedAsync(PaymentSql, PaymentParameters(scenario, Guid.NewGuid(), 10m, "same-key"));
        await InsertMobilePaymentAsync(scenario, 10m, "mobile-one", "TX-ONE");
        await AssertRejectedAsync(MobilePaymentSql, MobileParameters(scenario, Guid.NewGuid(), 10m, "mobile-two", "TX-ONE"));
    }

    [Test]
    public async Task ExactVersionAndCrossTenantRelationshipsAreRejected()
    {
        var scenario = await CreateScenarioAsync(); var wrongVersion = PaymentParameters(scenario, Guid.NewGuid(), 10m, "wrong-version").ToList(); wrongVersion[6] = ("version", 2);
        await AssertRejectedAsync(PaymentSql, wrongVersion.ToArray());
        var wrongTenant = PaymentParameters(scenario, Guid.NewGuid(), 10m, "wrong-tenant").ToList(); wrongTenant[1] = ("tenant", Guid.NewGuid());
        await AssertRejectedAsync(PaymentSql, wrongTenant.ToArray());
    }

    [Test]
    public async Task ConcurrentPaymentsCannotOverpayWorker()
    {
        var scenario = await CreateScenarioAsync();
        var results = await ConcurrentInsertAsync(PaymentSql,
            PaymentParameters(scenario, Guid.NewGuid(), 60m, "over-one"),
            PaymentParameters(scenario, Guid.NewGuid(), 60m, "over-two"));
        results.Count(x => x is null).ShouldBe(1); results.Count(x => x is PostgresException).ShouldBe(1);
        (await ActivePaymentCountAsync(scenario)).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentDuplicatePaymentCreatesOneAuthoritativeFact()
    {
        var scenario = await CreateScenarioAsync();
        var results = await ConcurrentInsertAsync(PaymentSql,
            PaymentParameters(scenario, Guid.NewGuid(), 10m, "duplicate-key"),
            PaymentParameters(scenario, Guid.NewGuid(), 10m, "duplicate-key"));
        results.Count(x => x is null).ShouldBe(1); results.Count(x => x is PostgresException).ShouldBe(1);
        (await ActivePaymentCountAsync(scenario)).ShouldBe(1);
    }

    private async Task<Scenario> CreateScenarioAsync()
    {
        var suffix = Guid.NewGuid().ToString("N"); var growerId = $"p6c-grower-{suffix}"; var managerId = $"p6c-manager-{suffix}"; var label = $"AUTOTEST-P6C-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{suffix}";
        var tenant = Tenant.CreateForGrower(growerId, label, null); var farm = tenant.CreateFarm($"P6C{suffix}"[..20], label, "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic");
        var manager = farm.AddPerson("Synthetic manager", null, new DateOnly(2037, 1, 1)); farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2037, 1, 1)); tenant.AddFarmManagerMembership(managerId, manager.Id); var workerPerson = farm.AddPerson("Synthetic worker", null, new DateOnly(2037, 1, 1));
        var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id, EmploymentType.Permanent, new DateOnly(2037, 1, 1), [1], new byte[12], new byte[16], "test-v1", new byte[32], "••••••12");
        var now = DateTimeOffset.UtcNow;
        var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2037, 1, now, managerId, manager.Id);
        period.Open(now, managerId, manager.Id, period.Version);
        await using (var context = Context())
        {
            context.Users.AddRange(User(growerId), User(managerId));
            context.Tenants.Add(tenant);
            context.WorkerProfiles.Add(worker);
            context.PayrollPeriods.Add(period);
            await context.SaveChangesAsync();
        }

        var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, now, managerId, manager.Id);
        int version;
        await using (var context = Context())
        {
            context.PayrollRuns.Add(run);
            await context.SaveChangesAsync();
            version = run.RecordCalculation(run.Version);
            await context.SaveChangesAsync();
        }

        var calculationId = Guid.NewGuid(); var lineId = Guid.NewGuid();
        await ExecuteAsync("INSERT INTO payroll.\"PayrollCalculations\" (\"Id\",\"PayrollRunId\",\"PayrollPeriodId\",\"TenantId\",\"FarmId\",\"CalculationVersion\",\"GrossAmountUsd\",\"DeductionAmountUsd\",\"NetAmountUsd\",\"EvidenceCount\",\"BlockerSnapshot\",\"SourceFingerprint\",\"CalculatedAt\",\"CalculatedByUserId\") VALUES (@calculation,@run,@period,@tenant,@farm,1,100,0,100,0,'[]','AUTOTEST-P6C',@now,@manager)", ("calculation", calculationId), ("run", run.Id), ("period", period.Id), ("tenant", tenant.Id), ("farm", farm.Id), ("now", now), ("manager", managerId));
        await ExecuteAsync("INSERT INTO payroll.\"PayrollWorkerLines\" (\"Id\",\"PayrollCalculationId\",\"TenantId\",\"FarmId\",\"WorkerProfileId\",\"WorkerNameSnapshot\",\"GrossAmountUsd\",\"DeductionAmountUsd\",\"NetAmountUsd\") VALUES (@line,@calculation,@tenant,@farm,@worker,'Synthetic worker',100,0,100)", ("line", lineId), ("calculation", calculationId), ("tenant", tenant.Id), ("farm", farm.Id), ("worker", worker.Id));
        long approvalRunVersion;
        await using (var context = Context())
        {
            PayrollRun persistedRun = await context.PayrollRuns.SingleAsync(item => item.Id == run.Id);
            persistedRun.Submit(version, now, managerId, persistedRun.Version);
            approvalRunVersion = persistedRun.Version;
            persistedRun.Decide(true, version, now, null, persistedRun.Version);
            await context.SaveChangesAsync();
        }

        await ExecuteAsync("INSERT INTO payroll.\"PayrollApprovals\" (\"Id\",\"PayrollRunId\",\"PayrollCalculationId\",\"TenantId\",\"FarmId\",\"RunVersion\",\"CalculationVersion\",\"Approved\",\"DecidedAt\",\"DecidedByUserId\",\"IdempotencyKey\") VALUES (@id,@run,@calculation,@tenant,@farm,@runVersion,@version,true,@now,@grower,@key)", ("id", Guid.NewGuid()), ("run", run.Id), ("calculation", calculationId), ("tenant", tenant.Id), ("farm", farm.Id), ("runVersion", approvalRunVersion), ("version", version), ("now", now), ("grower", growerId), ("key", $"approval-{suffix}"));
        await using (var context = Context())
        {
            PayrollPeriod persistedPeriod = await context.PayrollPeriods.SingleAsync(item => item.Id == period.Id);
            persistedPeriod.Close(now, growerId, null, run.Id, persistedPeriod.Version);
            await context.SaveChangesAsync();
        }

        return new(tenant.Id, farm.Id, run.Id, calculationId, lineId, worker.Id, managerId);
    }

    private async Task<Guid> InsertCashPaymentAsync(Scenario scenario, decimal amount, string key) { var id = Guid.NewGuid(); await ExecuteAsync(PaymentSql, PaymentParameters(scenario, id, amount, key)); return id; }
    private async Task<Guid> InsertMobilePaymentAsync(Scenario scenario, decimal amount, string key, string reference) { var id = Guid.NewGuid(); await ExecuteAsync(MobilePaymentSql, MobileParameters(scenario, id, amount, key, reference)); return id; }
    private async Task<Guid> InsertAcknowledgementAsync(Scenario scenario, Guid payment, string key) { var id = Guid.NewGuid(); await ExecuteAsync(AcknowledgementSql, ("id", id), ("payment", payment), ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("user", scenario.ManagerUserId), ("now", DateTimeOffset.UtcNow), ("key", key)); return id; }
    private async Task<Guid> InsertReversalAsync(Scenario scenario, Guid payment, decimal amount, string key) { var id = Guid.NewGuid(); await ExecuteAsync(ReversalSql, ScenarioParameters(scenario, payment, amount, key, id)); return id; }
    private async Task<Guid> InsertClosureAsync(Scenario scenario, string key) { var id = Guid.NewGuid(); await ExecuteAsync(ClosureSql, ClosureParameters(scenario, key, id)); return id; }

    private async Task<Exception?[]> ConcurrentInsertAsync(string sql, params (string Name, object Value)[][] parameterSets)
    { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var started = 0; return await Task.WhenAll(parameterSets.Select(async parameters => { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var transaction = await connection.BeginTransactionAsync(); if (Interlocked.Increment(ref started) == parameterSets.Length) ready.SetResult(); await ready.Task; try { await ExecuteAsync(connection, transaction, sql, parameters); await transaction.CommitAsync(); return null; } catch (Exception exception) { await transaction.RollbackAsync(); return exception; } })); }
    private async Task<int> ActivePaymentCountAsync(Scenario scenario) => Convert.ToInt32(await ScalarAsync("SELECT count(*) FROM payroll.\"PayrollPayments\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"PayrollRunId\"=@run", ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("run", scenario.RunId)));
    private async Task AssertRejectedAsync(string sql, params (string Name, object Value)[] parameters) => _ = await Should.ThrowAsync<PostgresException>(() => ExecuteAsync(sql, parameters));
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await ExecuteAsync(connection, null, sql, parameters); }
    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params (string Name, object Value)[] parameters) { await using var command = new NpgsqlCommand(sql, connection, transaction); foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value); await command.ExecuteNonQueryAsync(); }
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value); return await command.ExecuteScalarAsync(); }

    private static (string Name, object Value)[] PaymentParameters(Scenario s, Guid id, decimal amount, string key) => [("id", id), ("tenant", s.TenantId), ("farm", s.FarmId), ("run", s.RunId), ("calculation", s.CalculationId), ("line", s.WorkerLineId), ("version", 1), ("worker", s.WorkerId), ("amount", amount), ("user", s.ManagerUserId), ("now", DateTimeOffset.UtcNow), ("key", key), ("date", new DateOnly(2037, 1, 15))];
    private static (string Name, object Value)[] MobileParameters(Scenario s, Guid id, decimal amount, string key, string reference) => [.. PaymentParameters(s, id, amount, key), ("provider", "SyntheticProvider"), ("ciphertext", new byte[] { 1 }), ("nonce", new byte[12]), ("tag", new byte[16]), ("reference", reference)];
    private static (string Name, object Value)[] ScenarioParameters(Scenario s, Guid payment, decimal amount, string key, Guid? id = null) => [("id", id ?? Guid.NewGuid()), ("payment", payment), ("tenant", s.TenantId), ("farm", s.FarmId), ("run", s.RunId), ("calculation", s.CalculationId), ("line", s.WorkerLineId), ("version", 1), ("amount", amount), ("user", s.ManagerUserId), ("now", DateTimeOffset.UtcNow), ("key", key)];
    private static (string Name, object Value)[] ClosureParameters(Scenario s, string key, Guid? id = null) => [("id", id ?? Guid.NewGuid()), ("tenant", s.TenantId), ("farm", s.FarmId), ("run", s.RunId), ("calculation", s.CalculationId), ("user", s.ManagerUserId), ("now", DateTimeOffset.UtcNow), ("key", key)];

    private const string PaymentSql = "INSERT INTO payroll.\"PayrollPayments\" (\"Id\",\"TenantId\",\"FarmId\",\"PayrollRunId\",\"PayrollCalculationId\",\"CalculationVersion\",\"PayrollWorkerLineId\",\"WorkerProfileId\",\"Method\",\"AmountUsd\",\"PaymentDate\",\"ExternalStatus\",\"RecordedByUserId\",\"CreatedAt\",\"IdempotencyKey\",\"CorrelationId\") VALUES (@id,@tenant,@farm,@run,@calculation,@version,@line,@worker,'Cash',@amount,@date,'Posted',@user,@now,@key,'AUTOTEST-P6C')";
    private const string MobilePaymentSql = "INSERT INTO payroll.\"PayrollPayments\" (\"Id\",\"TenantId\",\"FarmId\",\"PayrollRunId\",\"PayrollCalculationId\",\"CalculationVersion\",\"PayrollWorkerLineId\",\"WorkerProfileId\",\"Method\",\"AmountUsd\",\"PaymentDate\",\"ExternalStatus\",\"Provider\",\"RecipientCiphertext\",\"RecipientNonce\",\"RecipientTag\",\"RecipientKeyId\",\"MaskedRecipientNumber\",\"TransactionReference\",\"RecordedByUserId\",\"CreatedAt\",\"IdempotencyKey\",\"CorrelationId\") VALUES (@id,@tenant,@farm,@run,@calculation,@version,@line,@worker,'MobileMoney',@amount,@date,'Successful',@provider,@ciphertext,@nonce,@tag,'test-v1','•••• 0123',@reference,@user,@now,@key,'AUTOTEST-P6C')";
    private const string AcknowledgementSql = "INSERT INTO payroll.\"PaymentAcknowledgements\" (\"Id\",\"PayrollPaymentId\",\"TenantId\",\"FarmId\",\"Status\",\"CapturedByUserId\",\"AcknowledgedAt\",\"CreatedAt\",\"IdempotencyKey\",\"CorrelationId\") VALUES (@id,@payment,@tenant,@farm,'Acknowledged',@user,@now,@now,@key,'AUTOTEST-P6C')";
    private const string ReversalSql = "INSERT INTO payroll.\"PayrollPaymentReversals\" (\"Id\",\"PayrollPaymentId\",\"TenantId\",\"FarmId\",\"PayrollRunId\",\"PayrollCalculationId\",\"CalculationVersion\",\"PayrollWorkerLineId\",\"AmountUsd\",\"Reason\",\"ReversedByUserId\",\"ReversedAt\",\"IdempotencyKey\",\"CorrelationId\") VALUES (@id,@payment,@tenant,@farm,@run,@calculation,@version,@line,@amount,'Synthetic correction',@user,@now,@key,'AUTOTEST-P6C')";
    private const string ClosureSql = "INSERT INTO payroll.\"PayrollSettlementClosures\" (\"Id\",\"TenantId\",\"FarmId\",\"PayrollRunId\",\"PayrollCalculationId\",\"CalculationVersion\",\"CloseSequence\",\"GrossAmountUsd\",\"DeductionAmountUsd\",\"NetAmountUsd\",\"ActivePaymentAmountUsd\",\"WorkerCount\",\"ClosedAt\",\"ClosedByUserId\",\"IdempotencyKey\",\"CorrelationId\") VALUES (@id,@tenant,@farm,@run,@calculation,1,1,100,0,100,100,1,@now,@user,@key,'AUTOTEST-P6C')";

    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid RunId, Guid CalculationId, Guid WorkerLineId, Guid WorkerId, string ManagerUserId);
}
