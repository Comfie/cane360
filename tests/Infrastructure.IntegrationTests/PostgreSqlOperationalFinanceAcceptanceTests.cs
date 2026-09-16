using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddOperationalFinanceAndCropCostProjection is explicitly approved and applied to Railway Development.")]
[Category("Phase7APostMigration")]
[NonParallelizable]
public sealed class PostgreSqlOperationalFinanceAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [Test]
    public async Task PostedTransactionUpdateIsRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await PostOnceAsync(scenario);
        await AssertRejectedAsync("UPDATE finance.\"OperationalTransactions\" SET \"AmountUsd\"=26 WHERE \"Id\"=@id", ("id", scenario.TransactionId));
    }

    [Test]
    public async Task PostedTransactionDeleteIsRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await PostOnceAsync(scenario);
        await AssertRejectedAsync("DELETE FROM finance.\"OperationalTransactions\" WHERE \"Id\"=@id", ("id", scenario.TransactionId));
    }

    [Test]
    public async Task CostPostingAppendOnlyMutationIsRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await PostOnceAsync(scenario);
        Guid postingId = (Guid)(await ScalarAsync("SELECT \"Id\" FROM finance.\"OperationalCostPostings\" WHERE \"TransactionAllocationId\"=@id", ("id", scenario.AllocationId)))!;
        await AssertRejectedAsync("UPDATE finance.\"OperationalCostPostings\" SET \"AmountUsd\"=1 WHERE \"Id\"=@id", ("id", postingId));
        await AssertRejectedAsync("DELETE FROM finance.\"OperationalCostPostings\" WHERE \"Id\"=@id", ("id", postingId));
    }

    [Test]
    public async Task TenantFarmCompositeForeignKeysAreEnforced()
    {
        (await CountAsync("SELECT count(*) FROM pg_class table_name JOIN pg_namespace schema_name ON schema_name.oid=table_name.relnamespace WHERE schema_name.nspname='finance' AND table_name.relname IN ('OperationalTransactions','TransactionAllocations','FinanceAuditEventLinks') AND table_name.relkind='r'")).ShouldBe(3);
        (await CountAsync("SELECT count(*) FROM information_schema.columns WHERE table_schema='finance' AND table_name='OperationalCostPostings' AND column_name IN ('PayrollEarningLineId','TransactionAllocationId')")).ShouldBe(2);
        (await CountAsync("SELECT count(*) FROM pg_constraint WHERE conrelid='finance.\"OperationalCostPostings\"'::regclass AND conname IN ('CK_OperationalCostPostings_OneSource','CK_OperationalCostPostings_SourceCategory','CK_OperationalCostPostings_Amount')")).ShouldBe(3);
        (await CountAsync("SELECT count(*) FROM pg_proc function JOIN pg_namespace schema_name ON schema_name.oid=function.pronamespace WHERE schema_name.nspname='finance' AND function.proname IN ('RejectLockedOperationalTransactionMutation','ValidateTransactionAllocationMutation','ValidateOperationalTransactionAllocationBalance','ValidateOperationalTransactionReversal','ValidateOperationalCostSource','RejectFinanceAuditLinkMutation')")).ShouldBe(6);
        (await CountAsync("SELECT count(*) FROM pg_trigger WHERE tgname IN ('TR_OperationalTransactions_RejectLockedMutation','TR_TransactionAllocations_ValidateMutation','TR_OperationalTransactions_AllocationBalance','TR_TransactionAllocations_AllocationBalance','TR_OperationalTransactions_ValidateReversal','TR_OperationalCostPostings_ValidateSource','TR_FinanceAuditEventLinks_AppendOnly') AND NOT tgisinternal")).ShouldBe(7);
        (await CountAsync("SELECT count(*) FROM pg_trigger trigger JOIN pg_proc function ON function.oid=trigger.tgfoid JOIN pg_namespace schema_name ON schema_name.oid=function.pronamespace WHERE trigger.tgrelid='finance.\"OperationalCostPostings\"'::regclass AND trigger.tgname='TR_OperationalCostPostings_AppendOnly' AND schema_name.nspname='inventory' AND function.proname='RejectAppendOnlyMutation' AND NOT trigger.tgisinternal")).ShouldBe(1);
        (await CountAsync("SELECT count(*) FROM pg_constraint WHERE connamespace='finance'::regnamespace AND contype='f' AND conrelid IN ('finance.\"OperationalTransactions\"'::regclass,'finance.\"TransactionAllocations\"'::regclass) AND pg_get_constraintdef(oid) LIKE '%\"TenantId\"%' AND pg_get_constraintdef(oid) LIKE '%\"FarmId\"%'")).ShouldBeGreaterThanOrEqualTo(3);
        string definition = Convert.ToString(await ScalarAsync(
            "SELECT pg_get_functiondef('finance.\"ValidateOperationalTransactionAllocationBalance\"()'::regprocedure)"))!;
        definition.ShouldContain("ELSIF TG_TABLE_NAME = 'OperationalTransactions'");
        definition.ShouldContain("transaction_id := NEW.\"Id\"");
        definition.ShouldContain("ELSIF TG_TABLE_NAME = 'TransactionAllocations'");
        definition.ShouldContain("transaction_id := NEW.\"OperationalTransactionId\"");
        definition.ShouldContain("transaction_id := OLD.\"OperationalTransactionId\"");
        definition.ShouldNotContain("transaction_id := CASE");
    }

    [Test]
    public async Task CrossTenantAllocationIsRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await AssertRejectedAsync(AllocationSql, AllocationParameters(scenario, Guid.NewGuid(), Guid.NewGuid(), scenario.FieldId, scenario.CropCycleId));
    }

    [Test]
    public async Task CropCycleFieldMismatchIsRejected()
    {
        FinanceScenario first = await CreateFinanceScenarioAsync();
        FinanceScenario second = await CreateFinanceScenarioAsync();
        await AssertRejectedAsync(AllocationSql, AllocationParameters(first, Guid.NewGuid(), first.TenantId, second.FieldId, first.CropCycleId));
    }

    [Test]
    public async Task PostingIdempotencyUniquenessIsEnforced() =>
        (await UniqueIndexExistsAsync("IX_OperationalTransactions_TenantId_FarmId_PostedIdempotencyKey")).ShouldBeTrue();

    [Test]
    public async Task ActiveSourcePostingUniquenessIsEnforced()
    {
        (await UniqueIndexExistsAsync("IX_OperationalCostPostings_TransactionAllocationId_CropCycleId~")).ShouldBeTrue();
        (await UniqueIndexExistsAsync("IX_OperationalCostPostings_PayrollEarningLineId_CropCycleId_Ca~")).ShouldBeTrue();
    }

    [Test]
    public async Task TransactionAllocationReconciliationGuardIsEnforced()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await ExecuteAsync("UPDATE finance.\"OperationalTransactions\" SET \"Notes\"='AUTOTEST-P7A trigger-shape regression' WHERE \"Id\"=@id", ("id", scenario.TransactionId));
        await ExecuteAsync("UPDATE finance.\"TransactionAllocations\" SET \"Category\"='Fuel' WHERE \"Id\"=@id", ("id", scenario.AllocationId));
        await ExecuteAsync("DELETE FROM finance.\"TransactionAllocations\" WHERE \"Id\"=@id", ("id", scenario.AllocationId));
        await ExecuteAsync(AllocationSql, AllocationParameters(scenario, scenario.AllocationId,
            scenario.TenantId, scenario.FieldId, scenario.CropCycleId));
        await ExecuteAsync("UPDATE finance.\"OperationalTransactions\" SET \"AmountUsd\"=26 WHERE \"Id\"=@id", ("id", scenario.TransactionId));
        await AssertRejectedAsync("UPDATE finance.\"OperationalTransactions\" SET \"Status\"='Posted',\"PostedByUserId\"=@user,\"PostedAt\"=@now,\"PostedIdempotencyKey\"=@key WHERE \"Id\"=@id",
            ("id", scenario.TransactionId), ("user", scenario.GrowerUserId),
            ("now", DateTimeOffset.UtcNow), ("key", $"AUTOTEST-P7A-{Guid.NewGuid():N}"));
    }

    [Test]
    public async Task IncomeCannotCreateDirectExpenseCost()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await ExecuteAsync("UPDATE finance.\"OperationalTransactions\" SET \"Type\"='Income' WHERE \"Id\"=@id", ("id", scenario.TransactionId));
        await AssertRejectedAsync(PostSql, PostParameters(scenario, Guid.NewGuid()));
    }

    [Test]
    public async Task ReversalSourceIntegrityIsEnforced()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await PostOnceAsync(scenario);
        Guid original = (Guid)(await ScalarAsync("SELECT \"Id\" FROM finance.\"OperationalCostPostings\" WHERE \"TransactionAllocationId\"=@id", ("id", scenario.AllocationId)))!;
        await AssertRejectedAsync(ReversalPostingSql, ("id", Guid.NewGuid()), ("original", original),
            ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("field", scenario.FieldId),
            ("cycle", scenario.CropCycleId), ("allocation", scenario.AllocationId),
            ("identity", $"AUTOTEST-P7A-{Guid.NewGuid():N}"));
    }

    [Test]
    public async Task DuplicatePayrollCostSourceIsRejected() =>
        (await UniqueIndexExistsAsync("IX_OperationalCostPostings_PayrollEarningLineId_CropCycleId_Ca~")).ShouldBeTrue();

    [Test]
    public async Task DuplicateDirectExpenseSourceIsRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await PostOnceAsync(scenario);
        await AssertRejectedAsync(DirectPostingSql, ("id", Guid.NewGuid()),
            ("tenant", scenario.TenantId), ("farm", scenario.FarmId),
            ("field", scenario.FieldId), ("cycle", scenario.CropCycleId),
            ("allocation", scenario.AllocationId),
            ("identity", $"AUTOTEST-P7A-{Guid.NewGuid():N}"));
    }

    [Test]
    public async Task ConcurrentTransactionPostingCreatesOneAuthoritativeSet()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        Exception?[] results = await ConcurrentAsync(PostSql, PostParameters(scenario, Guid.NewGuid()), PostParameters(scenario, Guid.NewGuid()));
        results.Count(x => x is null).ShouldBe(1);
        results.Count(x => x is PostgresException).ShouldBe(1);
        (await CountAsync("SELECT count(*) FROM finance.\"OperationalTransactions\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"Id\"=@transaction AND \"Status\"='Posted'", ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("transaction", scenario.TransactionId))).ShouldBe(1);
        (await DecimalAsync("SELECT COALESCE(sum(\"AmountUsd\"),0) FROM finance.\"OperationalCostPostings\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"TransactionAllocationId\"=@allocation", ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("allocation", scenario.AllocationId))).ShouldBe(25m);
    }

    [Test]
    public async Task ConcurrentPayrollReconciliationCreatesOneAuthoritativePosting()
    {
        PayrollScenario scenario = await CreatePayrollScenarioAsync();
        Exception?[] results = await ConcurrentAsync(PayrollPostingSql,
            PayrollPostingParameters(scenario, Guid.NewGuid()), PayrollPostingParameters(scenario, Guid.NewGuid()));
        results.Count(x => x is null).ShouldBe(1);
        results.Count(x => x is PostgresException).ShouldBe(1);
        (await CountAsync("SELECT count(*) FROM finance.\"OperationalCostPostings\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"PayrollEarningLineId\"=@earning AND \"ReversalOfOperationalCostPostingId\" IS NULL", ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("earning", scenario.EarningLineId))).ShouldBe(1);
        (await DecimalAsync("SELECT COALESCE(sum(\"AmountUsd\"),0) FROM finance.\"OperationalCostPostings\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"PayrollEarningLineId\"=@earning", ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("earning", scenario.EarningLineId))).ShouldBe(20m);
    }

    [Test]
    public async Task ClosedCycleOrdinaryPostingIsDatabaseRejected()
    {
        FinanceScenario scenario = await CreateFinanceScenarioAsync();
        await ExecuteAsync("UPDATE farm.\"CropCycles\" SET \"Status\"='Closed' WHERE \"Id\"=@id", ("id", scenario.CropCycleId));
        await AssertRejectedAsync(PostSql, PostParameters(scenario, Guid.NewGuid()));
    }

    private async Task<FinanceScenario> CreateFinanceScenarioAsync()
    {
        BaseScenario source = await CreateBaseScenarioAsync(false);
        var transaction = OperationalTransaction.Create(source.TenantId, source.FarmId,
            OperationalTransactionType.Expense, OperationalFinanceCategory.ContractServices,
            new DateOnly(2038, 1, 10), "AUTOTEST-P7A supplier", 25m, "AUTOTEST-P7A",
            "Synthetic gated finance fact", source.GrowerUserId, DateTimeOffset.UtcNow,
            $"AUTOTEST-P7A-{Guid.NewGuid():N}");
        var allocation = TransactionAllocation.Create(source.TenantId, source.FarmId,
            transaction.Id, source.CropCycleId, source.FieldId,
            OperationalFinanceCategory.ContractServices, 25m,
            TransactionAllocationType.CropCycleDirect, DateTimeOffset.UtcNow);
        transaction.ReplaceAllocations([allocation], transaction.Version);
        await using var context = Context();
        context.OperationalTransactions.Add(transaction);
        await context.SaveChangesAsync();
        return new(source.TenantId, source.FarmId, source.FieldId, source.CropCycleId,
            source.GrowerUserId, transaction.Id, allocation.Id);
    }

    private async Task<PayrollScenario> CreatePayrollScenarioAsync()
    {
        BaseScenario source = await CreateBaseScenarioAsync(true);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateOnly workDate = new(2038, 1, 10);
        var attendance = Attendance.Create(source.TenantId, source.FarmId, source.WorkerId!.Value,
            workDate, AttendanceStatus.Present, source.FieldId, now, source.ManagerUserId!, null, 0);
        var rate = WorkerRate.Create(source.TenantId, source.FarmId, source.WorkerId.Value,
            PayBasis.Hectare, source.ActivityTypeId, 10m, new DateOnly(2038, 1, 1), null);
        var work = WorkRecord.Create(source.TenantId, source.FarmId, attendance.Id,
            source.WorkerId.Value, source.FieldId, workDate, rate, 2m,
            [source.ActivityId!.Value], now, source.ManagerUserId!, null, 0);
        work.RecordSupervisorVerification(source.SupervisorPersonId!.Value, now,
            source.ManagerUserId!, work.Version);
        work.Confirm(now, source.ManagerUserId!, work.Version);
        var period = PayrollPeriod.Create(source.TenantId, source.FarmId, 2038, 1, now,
            source.ManagerUserId!, source.ManagerPersonId);
        period.Open(now, source.ManagerUserId!, source.ManagerPersonId, period.Version);
        var run = PayrollRun.Create(source.TenantId, source.FarmId, period.Id, now,
            source.ManagerUserId!, source.ManagerPersonId);
        int calculationVersion = run.RecordCalculation(run.Version);
        Guid calculationId = Guid.NewGuid();
        Guid workerLineId = Guid.NewGuid();
        var earning = PayrollEarningLine.Create(workerLineId, calculationId, source.TenantId,
            source.FarmId, source.WorkerId.Value, work.Id, "WorkRecord", workDate,
            attendance.Id, attendance.Version, work.Verification!.SupervisorVerifiedAt,
            work.Verification.ManagerConfirmedAt!.Value, source.FieldId, "[]", 2m, "ha",
            "Hectare", 10m, rate.Id, rate.Version, "AUTOTEST-P7A-EARNING");
        var workerLine = PayrollWorkerLine.Create(workerLineId, calculationId, source.TenantId,
            source.FarmId, source.WorkerId.Value, "Synthetic worker", [earning], []);
        var calculation = PayrollCalculation.Create(calculationId, run.Id, period.Id,
            source.TenantId, source.FarmId, calculationVersion, [workerLine], [],
            "AUTOTEST-P7A-CALCULATION", now, source.ManagerUserId!, source.ManagerPersonId);
        run.Submit(calculationVersion, now, source.ManagerUserId!, run.Version);
        long approvalVersion = run.Version;
        run.Decide(true, calculationVersion, now, null, run.Version);
        var approval = PayrollApproval.Create(run.Id, calculation.Id, source.TenantId,
            source.FarmId, approvalVersion, calculationVersion, true, null, now,
            source.GrowerUserId, null, $"AUTOTEST-P7A-{Guid.NewGuid():N}");
        await using var context = Context();
        context.Attendances.Add(attendance); context.WorkerRates.Add(rate);
        context.WorkRecords.Add(work); context.PayrollPeriods.Add(period);
        context.PayrollRuns.Add(run); context.PayrollCalculations.Add(calculation);
        context.PayrollApprovals.Add(approval);
        await context.SaveChangesAsync();
        return new(source.TenantId, source.FarmId, source.FieldId, source.CropCycleId,
            source.ActivityId.Value, earning.Id);
    }

    private async Task<BaseScenario> CreateBaseScenarioAsync(bool includeLabour)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string label = $"AUTOTEST-P7A-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
        string growerId = $"p7a-grower-{suffix}";
        string? managerId = includeLabour ? $"p7a-manager-{suffix}" : null;
        var tenant = Tenant.CreateForGrower(growerId, label, null);
        CropVariety variety = tenant.AddCropVariety($"V{suffix}"[..20], "Synthetic cane");
        Farm farm = tenant.CreateFarm($"F{suffix}"[..20], label, "Synthetic address",
            "Railway Development", "Synthetic", 10m, "Synthetic");
        Person? manager = null; Person? supervisor = null; WorkerProfile? worker = null;
        if (includeLabour)
        {
            manager = farm.AddPerson("Synthetic manager", null, new DateOnly(2038, 1, 1));
            supervisor = farm.AddPerson("Synthetic supervisor", null, new DateOnly(2038, 1, 1));
            Person workerPerson = farm.AddPerson("Synthetic worker", null, new DateOnly(2038, 1, 1));
            farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2038, 1, 1));
            farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2038, 1, 1));
            tenant.AddMembership(managerId!, manager.Id, TenantSecurityRoles.FarmManager);
            worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id,
                EmploymentType.Seasonal, new DateOnly(2038, 1, 1), [1], new byte[12],
                new byte[16], "test-v1", new byte[32], "••••••12");
        }
        Field field = farm.AddField("P7A", "Synthetic field", 10m, null,
            ReportingAreaSource.Declared, "Synthetic", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2038, 1, 1), new DateOnly(2038, 11, 1),
            new DateOnly(2039, 1, 31), 100m, DateTimeOffset.UtcNow, growerId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, growerId);
        ActivityType? activityType = null; Activity? activity = null;
        if (includeLabour)
        {
            activityType = tenant.AddActivityType($"A{suffix}"[..20], "Synthetic labour",
                true, true, ActivityQuantityBasis.Hectares);
            activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, activityType,
                ActivityPlanningKind.Planned, new DateOnly(2038, 1, 10), supervisor!.Id);
        }
        await using var context = Context();
        context.Users.Add(User(growerId));
        if (includeLabour) context.Users.Add(User(managerId!));
        context.Tenants.Add(tenant);
        if (worker is not null) context.WorkerProfiles.Add(worker);
        await context.SaveChangesAsync();
        return new(tenant.Id, farm.Id, field.Id, cycle.Id, growerId, managerId,
            manager?.Id, supervisor?.Id, worker?.Id, activityType?.Id, activity?.Id);
    }

    private async Task PostOnceAsync(FinanceScenario scenario) =>
        await ExecuteAsync(PostSql, PostParameters(scenario, Guid.NewGuid()));

    private async Task<Exception?[]> ConcurrentAsync(string sql,
        params (string Name, object Value)[][] parameterSets)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        return await Task.WhenAll(parameterSets.Select(async parameters =>
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            if (Interlocked.Increment(ref started) == parameterSets.Length) ready.SetResult();
            await ready.Task;
            try
            {
                await ExecuteAsync(connection, transaction, sql, parameters);
                await transaction.CommitAsync();
                return null;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                return exception;
            }
        }));
    }

    private async Task<bool> UniqueIndexExistsAsync(string name) => Convert.ToBoolean(await ScalarAsync(
        "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname='finance' AND indexname=@name AND indexdef LIKE 'CREATE UNIQUE INDEX%')", ("name", name)));
    private async Task AssertRejectedAsync(string sql, params (string Name, object Value)[] parameters) =>
        _ = await Should.ThrowAsync<PostgresException>(() => ExecuteAsync(sql, parameters));
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await ExecuteAsync(connection, null, sql, parameters); }
    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string sql, params (string Name, object Value)[] parameters)
    { await using var command = new NpgsqlCommand(sql, connection, transaction); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
    private async Task<int> CountAsync(string sql, params (string Name, object Value)[] parameters) => Convert.ToInt32(await ScalarAsync(sql, parameters));
    private async Task<decimal> DecimalAsync(string sql, params (string Name, object Value)[] parameters) => Convert.ToDecimal(await ScalarAsync(sql, parameters));
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private static string LoadConfiguredConnectionString() { string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }

    private static (string Name, object Value)[] PostParameters(FinanceScenario source, Guid postingId) =>
        [("transaction", source.TransactionId), ("allocation", source.AllocationId), ("tenant", source.TenantId),
         ("farm", source.FarmId), ("field", source.FieldId), ("cycle", source.CropCycleId),
         ("user", source.GrowerUserId), ("now", DateTimeOffset.UtcNow), ("key", $"AUTOTEST-P7A-{postingId:N}"), ("posting", postingId)];
    private static (string Name, object Value)[] AllocationParameters(FinanceScenario source,
        Guid id, Guid tenantId, Guid fieldId, Guid cycleId) => [("id", id), ("tenant", tenantId),
        ("farm", source.FarmId), ("transaction", source.TransactionId), ("field", fieldId),
        ("cycle", cycleId), ("now", DateTimeOffset.UtcNow)];
    private static (string Name, object Value)[] PayrollPostingParameters(PayrollScenario source, Guid id) =>
        [("id", id), ("tenant", source.TenantId), ("farm", source.FarmId), ("field", source.FieldId),
         ("cycle", source.CropCycleId), ("activity", source.ActivityId), ("earning", source.EarningLineId),
         ("identity", $"AUTOTEST-P7A-{id:N}")];

    private const string AllocationSql = "INSERT INTO finance.\"TransactionAllocations\" (\"Id\",\"TenantId\",\"FarmId\",\"OperationalTransactionId\",\"CropCycleId\",\"FieldId\",\"Category\",\"AmountUsd\",\"AllocationType\",\"CreatedAt\") VALUES (@id,@tenant,@farm,@transaction,@cycle,@field,'ContractServices',25,'CropCycleDirect',@now)";
    private const string PostSql = "UPDATE finance.\"OperationalTransactions\" SET \"Status\"='Posted',\"Version\"=\"Version\"+1,\"PostedByUserId\"=@user,\"PostedAt\"=@now,\"PostedIdempotencyKey\"=@key WHERE \"Id\"=@transaction; INSERT INTO finance.\"OperationalCostPostings\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"CropCycleId\",\"Category\",\"TransactionAllocationId\",\"SourceQuantitySnapshot\",\"UnitCostUsdSnapshot\",\"AmountUsd\",\"PostingIdentity\") VALUES (@posting,@tenant,@farm,@field,@cycle,'DirectExpense',@allocation,1,25,25,@key)";
    private const string PayrollPostingSql = "INSERT INTO finance.\"OperationalCostPostings\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"ActivityId\",\"CropCycleId\",\"Category\",\"PayrollEarningLineId\",\"SourceQuantitySnapshot\",\"UnitCostUsdSnapshot\",\"AmountUsd\",\"PostingIdentity\") VALUES (@id,@tenant,@farm,@field,@activity,@cycle,'Labour',@earning,2,10,20,@identity)";
    private const string DirectPostingSql = "INSERT INTO finance.\"OperationalCostPostings\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"CropCycleId\",\"Category\",\"TransactionAllocationId\",\"SourceQuantitySnapshot\",\"UnitCostUsdSnapshot\",\"AmountUsd\",\"PostingIdentity\") VALUES (@id,@tenant,@farm,@field,@cycle,'DirectExpense',@allocation,1,25,25,@identity)";
    private const string ReversalPostingSql = "INSERT INTO finance.\"OperationalCostPostings\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"CropCycleId\",\"Category\",\"TransactionAllocationId\",\"SourceQuantitySnapshot\",\"UnitCostUsdSnapshot\",\"AmountUsd\",\"PostingIdentity\",\"ReversalOfOperationalCostPostingId\") VALUES (@id,@tenant,@farm,@field,@cycle,'DirectExpense',@allocation,1,-24,-24,@identity,@original)";

    private sealed record BaseScenario(Guid TenantId, Guid FarmId, Guid FieldId, Guid CropCycleId,
        string GrowerUserId, string? ManagerUserId, Guid? ManagerPersonId, Guid? SupervisorPersonId,
        Guid? WorkerId, Guid? ActivityTypeId, Guid? ActivityId);
    private sealed record FinanceScenario(Guid TenantId, Guid FarmId, Guid FieldId, Guid CropCycleId,
        string GrowerUserId, Guid TransactionId, Guid AllocationId);
    private sealed record PayrollScenario(Guid TenantId, Guid FarmId, Guid FieldId, Guid CropCycleId,
        Guid ActivityId, Guid EarningLineId);
}
