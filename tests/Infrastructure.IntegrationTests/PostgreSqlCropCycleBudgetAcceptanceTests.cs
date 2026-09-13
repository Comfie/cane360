using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddCropCycleBudgetsAndVarianceReporting is explicitly approved and applied to Railway Development.")]
[Category("Phase7BPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlCropCycleBudgetAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [Test]
    public async Task ApprovedBudgetUpdateIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync("UPDATE finance.\"Budgets\" SET \"Name\"='AUTOTEST-P7B changed' WHERE \"Id\"=@id", ("id", source.BudgetId));
    }

    [Test]
    public async Task ApprovedBudgetDeleteIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync("DELETE FROM finance.\"Budgets\" WHERE \"Id\"=@id", ("id", source.BudgetId));
    }

    [Test]
    public async Task ApprovedBudgetLineUpdateIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync("UPDATE finance.\"BudgetLines\" SET \"AmountUsd\"=2 WHERE \"Id\"=@id", ("id", source.LineId));
    }

    [Test]
    public async Task ApprovedBudgetLineDeleteIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync("DELETE FROM finance.\"BudgetLines\" WHERE \"Id\"=@id", ("id", source.LineId));
    }

    [Test]
    public async Task TenantFarmCropCycleForeignKeysAreEnforced()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Draft);
        await AssertRejectedAsync(InsertBudgetSql, BudgetParameters(source with
            { BudgetId = Guid.NewGuid(), FarmId = Guid.NewGuid(), Version = 2 }, null));
    }

    [Test]
    public async Task CrossTenantRevisionIsRejected()
    {
        BudgetScenario first = await CreateScenarioAsync(BudgetStatus.Approved);
        BudgetScenario second = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync(InsertBudgetSql, BudgetParameters(second with
            { BudgetId = Guid.NewGuid(), Version = 2 }, first.BudgetId));
    }

    [Test]
    public async Task UniqueCropCycleVersionIsEnforced()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Draft);
        await AssertRejectedAsync(InsertBudgetSql, BudgetParameters(source with
            { BudgetId = Guid.NewGuid() }, null));
    }

    [Test]
    public async Task SingleCurrentApprovedBudgetIsEnforced()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync(InsertApprovedBudgetSql, BudgetParameters(source with
            { BudgetId = Guid.NewGuid(), Version = 2 }, source.BudgetId));
    }

    [Test]
    public async Task RevisionLineageIntegrityIsEnforced()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        await AssertRejectedAsync(InsertBudgetSql, BudgetParameters(source with
            { BudgetId = Guid.NewGuid(), Version = 3 }, source.BudgetId));
    }

    [Test]
    public async Task InvalidApprovalLifecycleIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Draft);
        await AssertRejectedAsync("UPDATE finance.\"Budgets\" SET \"Status\"='Approved',\"ApprovedAt\"=@now,\"ApprovedByUserId\"=@user,\"ApprovalIdempotencyKey\"=@key WHERE \"Id\"=@id",
            ("now", DateTimeOffset.UtcNow), ("user", source.UserId),
            ("key", $"AUTOTEST-P7B-{Guid.NewGuid():N}"), ("id", source.BudgetId));
    }

    [Test]
    public async Task ConcurrentRevisionCreationProducesOneNextVersion()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Approved);
        (string Name, object Value)[] first = BudgetParameters(source with
            { BudgetId = Guid.NewGuid(), Version = 2 }, source.BudgetId);
        (string Name, object Value)[] second = BudgetParameters(source with
            { BudgetId = Guid.NewGuid(), Version = 2 }, source.BudgetId);
        Exception?[] results = await ConcurrentAsync(InsertBudgetSql, first, second);
        results.Count(x => x is null).ShouldBe(1);
        (await CountAsync("SELECT count(*) FROM finance.\"Budgets\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"CropCycleId\"=@cycle AND \"Version\"=2",
            ("tenant", source.TenantId), ("farm", source.FarmId), ("cycle", source.CropCycleId))).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentApprovalProducesOneCurrentApprovedVersion()
    {
        BudgetScenario current = await CreateScenarioAsync(BudgetStatus.Approved);
        BudgetScenario revision = current with { BudgetId = Guid.NewGuid(), LineId = Guid.NewGuid(), Version = 2 };
        await ExecuteAsync(InsertBudgetSql, BudgetParameters(revision, current.BudgetId));
        await ExecuteAsync(InsertLineSql, LineParameters(revision));
        await ExecuteAsync("UPDATE finance.\"Budgets\" SET \"Status\"='Submitted',\"SubmittedByUserId\"=@user,\"SubmittedAt\"=@now,\"RowVersion\"=2 WHERE \"Id\"=@id",
            ("user", revision.UserId), ("now", DateTimeOffset.UtcNow), ("id", revision.BudgetId));
        string sql = "UPDATE finance.\"Budgets\" SET \"Status\"='Superseded',\"RowVersion\"=\"RowVersion\"+1 WHERE \"Id\"=@current AND \"Status\"='Approved'; UPDATE finance.\"Budgets\" SET \"Status\"='Approved',\"ApprovedByUserId\"=@user,\"ApprovedAt\"=@now,\"ApprovalIdempotencyKey\"=@key,\"RowVersion\"=\"RowVersion\"+1 WHERE \"Id\"=@revision AND \"Status\"='Submitted'";
        Exception?[] results = await ConcurrentAsync(sql,
            ApprovalParameters(current, revision, Guid.NewGuid()),
            ApprovalParameters(current, revision, Guid.NewGuid()));
        results.Count(x => x is null).ShouldBeGreaterThanOrEqualTo(1);
        (await CountAsync("SELECT count(*) FROM finance.\"Budgets\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"CropCycleId\"=@cycle AND \"Status\"='Approved'",
            ("tenant", current.TenantId), ("farm", current.FarmId), ("cycle", current.CropCycleId))).ShouldBe(1);
        Convert.ToInt32(await ScalarAsync("SELECT \"Version\" FROM finance.\"Budgets\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"CropCycleId\"=@cycle AND \"Status\"='Approved'",
            ("tenant", current.TenantId), ("farm", current.FarmId), ("cycle", current.CropCycleId))).ShouldBe(2);
    }

    [Test]
    public async Task ApprovalIdempotencyUniquenessIsEnforced()
    {
        (await UniqueIndexExistsAsync("IX_Budgets_TenantId_FarmId_ApprovalIdempotencyKey"))
            .ShouldBeTrue();
    }

    [Test]
    public async Task ApprovedLineCategoryIntegrityIsEnforced()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Draft);
        await AssertRejectedAsync(InsertLineSql.Replace("'Labour'", "'Unsupported'", StringComparison.Ordinal),
            LineParameters(source with { LineId = Guid.NewGuid() }));
    }

    [Test]
    public async Task ClosedCycleOrdinaryBudgetCreationIsRejected()
    {
        BudgetScenario source = await CreateScenarioAsync(BudgetStatus.Draft);
        await ExecuteAsync("DELETE FROM finance.\"BudgetLines\" WHERE \"BudgetId\"=@id", ("id", source.BudgetId));
        await ExecuteAsync("DELETE FROM finance.\"Budgets\" WHERE \"Id\"=@id", ("id", source.BudgetId));
        await ExecuteAsync("UPDATE farm.\"CropCycles\" SET \"Status\"='Closed' WHERE \"Id\"=@id", ("id", source.CropCycleId));
        await AssertRejectedAsync(InsertBudgetSql, BudgetParameters(source with
            { BudgetId = Guid.NewGuid() }, null));
    }

    [Test]
    public async Task FinanceAuditLinkAppendOnlyProtectionRemainsActive()
    {
        (await CountAsync("SELECT count(*) FROM pg_trigger WHERE tgrelid='finance.\"FinanceAuditEventLinks\"'::regclass AND tgname='TR_FinanceAuditEventLinks_AppendOnly' AND NOT tgisinternal"))
            .ShouldBe(1);
    }

    private async Task<BudgetScenario> CreateScenarioAsync(BudgetStatus status)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string label = $"AUTOTEST-P7B-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
        string userId = $"p7b-grower-{suffix}";
        Tenant tenant = Tenant.CreateForGrower(userId, label, null);
        CropVariety variety = tenant.AddCropVariety($"V{suffix}"[..20], "Synthetic cane");
        Farm farm = tenant.CreateFarm($"F{suffix}"[..20], label, "Synthetic address",
            "Railway Development", "Synthetic", 10m, "Synthetic");
        Field field = farm.AddField("P7B", "Synthetic field", 10m, null,
            ReportingAreaSource.Declared, "Synthetic", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2039, 1, 1), new DateOnly(2039, 11, 1),
            new DateOnly(2040, 1, 31), 100m, DateTimeOffset.UtcNow, userId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, userId);
        await using (ApplicationDbContext context = Context())
        {
            context.Users.Add(User(userId));
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }
        Budget budget = Budget.CreateDraft(tenant.Id, farm.Id, field.Id, cycle.Id, 1,
            label, 10m, 100m, "Synthetic gated budget", userId, DateTimeOffset.UtcNow);
        BudgetLine line = budget.AddLine(BudgetCategory.Labour, "Synthetic labour", 100m,
            null, null, null, null, DateTimeOffset.UtcNow, budget.RowVersion);
        await using (ApplicationDbContext context = Context())
        {
            context.Budgets.Add(budget);
            await context.SaveChangesAsync();
            if (status is BudgetStatus.Submitted or BudgetStatus.Approved)
            {
                budget.Submit(userId, DateTimeOffset.UtcNow, budget.RowVersion);
                await context.SaveChangesAsync();
            }
            if (status == BudgetStatus.Approved)
            {
                budget.Approve(userId, DateTimeOffset.UtcNow,
                    $"AUTOTEST-P7B-{Guid.NewGuid():N}", budget.RowVersion);
                await context.SaveChangesAsync();
            }
        }
        return new(tenant.Id, farm.Id, field.Id, cycle.Id, userId, budget.Id, line.Id, 1);
    }

    private async Task<Exception?[]> ConcurrentAsync(string sql,
        params (string Name, object Value)[][] parameterSets)
    {
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        return await Task.WhenAll(parameterSets.Select(async parameters =>
        {
            await using NpgsqlConnection connection = new(_connectionString);
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
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
    { await using NpgsqlConnection connection = new(_connectionString); await connection.OpenAsync(); await ExecuteAsync(connection, null, sql, parameters); }
    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string sql, params (string Name, object Value)[] parameters)
    { await using NpgsqlCommand command = new(sql, connection, transaction); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    { await using NpgsqlConnection connection = new(_connectionString); await connection.OpenAsync(); await using NpgsqlCommand command = new(sql, connection); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
    private async Task<int> CountAsync(string sql, params (string Name, object Value)[] parameters) => Convert.ToInt32(await ScalarAsync(sql, parameters));
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private static string LoadConfiguredConnectionString() { string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }

    private static (string Name, object Value)[] BudgetParameters(BudgetScenario source, Guid? supersedes) =>
        [("id", source.BudgetId), ("tenant", source.TenantId), ("farm", source.FarmId),
         ("field", source.FieldId), ("cycle", source.CropCycleId), ("version", source.Version),
         ("name", $"AUTOTEST-P7B-v{source.Version}-{source.BudgetId:N}"), ("user", source.UserId),
         ("now", DateTimeOffset.UtcNow), ("supersedes", supersedes ?? (object)DBNull.Value)];
    private static (string Name, object Value)[] LineParameters(BudgetScenario source) =>
        [("id", source.LineId), ("tenant", source.TenantId), ("farm", source.FarmId),
         ("budget", source.BudgetId), ("now", DateTimeOffset.UtcNow)];
    private static (string Name, object Value)[] ApprovalParameters(BudgetScenario current,
        BudgetScenario revision, Guid key) => [("current", current.BudgetId),
        ("revision", revision.BudgetId), ("user", revision.UserId),
        ("now", DateTimeOffset.UtcNow), ("key", $"AUTOTEST-P7B-{key:N}")];

    private const string InsertBudgetSql = "INSERT INTO finance.\"Budgets\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"CropCycleId\",\"Version\",\"Status\",\"Name\",\"ReportingAreaHa\",\"ExpectedProductionTonnes\",\"CreatedByUserId\",\"CreatedAt\",\"SupersedesBudgetId\",\"RowVersion\") VALUES (@id,@tenant,@farm,@field,@cycle,@version,'Draft',@name,10,100,@user,@now,@supersedes,0)";
    private const string InsertApprovedBudgetSql = "INSERT INTO finance.\"Budgets\" (\"Id\",\"TenantId\",\"FarmId\",\"FieldId\",\"CropCycleId\",\"Version\",\"Status\",\"Name\",\"ReportingAreaHa\",\"ExpectedProductionTonnes\",\"CreatedByUserId\",\"CreatedAt\",\"SubmittedByUserId\",\"SubmittedAt\",\"ApprovedByUserId\",\"ApprovedAt\",\"ApprovalIdempotencyKey\",\"SupersedesBudgetId\",\"RowVersion\") VALUES (@id,@tenant,@farm,@field,@cycle,@version,'Approved',@name,10,100,@user,@now,@user,@now,@user,@now,@name,@supersedes,3)";
    private const string InsertLineSql = "INSERT INTO finance.\"BudgetLines\" (\"Id\",\"TenantId\",\"FarmId\",\"BudgetId\",\"Category\",\"Description\",\"AmountUsd\",\"CreatedAt\") VALUES (@id,@tenant,@farm,@budget,'Labour','AUTOTEST-P7B synthetic',100,@now)";

    private sealed record BudgetScenario(Guid TenantId, Guid FarmId, Guid FieldId,
        Guid CropCycleId, string UserId, Guid BudgetId, Guid LineId, int Version);
}
