using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
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
[Explicit("Run only after AddPayrollFoundationsWorkerAdvancesAndPreflight is explicitly approved and applied to Railway Development.")]
[Category("Phase6APostMigration")]
[NonParallelizable]
public sealed class PostgreSqlPayrollFoundationsAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
        _runId = $"AUTOTEST-P6A-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    [Test]
    public async Task PayrollPeriodFarmMonthIsUnique()
    {
        var scenario = await CreateScenarioAsync(); await using var context = CreateContext();
        context.PayrollPeriods.Add(PayrollPeriod.Create(scenario.TenantId, scenario.FarmId, 2026, 9, DateTimeOffset.UtcNow, scenario.GrowerUserId, null));
        var exception = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
        ((PostgresException)exception.InnerException!).ConstraintName.ShouldBe("UX_PayrollPeriods_Farm_Year_Month");
    }

    [Test]
    public async Task PayrollPeriodDatesMustMatchCalendarMonth()
    {
        var scenario = await CreateScenarioAsync();
        await AssertSqlConstraintAsync("UPDATE payroll.\"PayrollPeriods\" SET \"EndDate\" = \"EndDate\" - 1 WHERE \"Id\" = @id", scenario.PeriodIds[0]);
    }

    [Test]
    public async Task PayrollRelationshipsRejectCrossTenantForeignKeys()
    {
        var scenario = await CreateScenarioAsync();
        await AssertSqlConstraintAsync("UPDATE payroll.\"AdvanceInstallments\" SET \"TenantId\" = @other WHERE \"WorkerAdvanceId\" = @id", scenario.AdvanceId, Guid.NewGuid());
    }

    [Test]
    public async Task PayrollQueriesAreTenantAndFarmIsolated()
    {
        var scenario = await CreateScenarioAsync(); await using var context = CreateContext(); var repository = new PayrollRepository(context);
        (await repository.GetAdvanceAsync(Guid.NewGuid(), scenario.FarmId, scenario.AdvanceId, false, CancellationToken.None)).ShouldBeNull();
        (await repository.GetAdvanceAsync(scenario.TenantId, Guid.NewGuid(), scenario.AdvanceId, false, CancellationToken.None)).ShouldBeNull();
    }

    [Test]
    public async Task GrowerApprovalBindsExactAdvanceVersion()
    {
        var scenario = await CreateScenarioAsync(); var result = await DecideAsync(scenario, scenario.AdvanceVersion, true, "exact-version");
        result.Status.ShouldBe("Approved"); await using var verify = CreateContext();
        (await verify.AdvanceApprovals.SingleAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).AdvanceVersion.ShouldBe(scenario.AdvanceVersion);
    }

    [Test]
    public async Task ApprovalRetryIsIdempotent()
    {
        var scenario = await CreateScenarioAsync(); await DecideAsync(scenario, scenario.AdvanceVersion, true, "approval-retry"); await DecideAsync(scenario, scenario.AdvanceVersion, true, "approval-retry");
        await using var verify = CreateContext(); (await verify.AdvanceApprovals.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1);
    }

    [Test]
    public async Task FarmManagerApprovalIsForbidden()
    {
        var scenario = await CreateScenarioAsync(); await using var context = CreateContext();
        var handler = new DecideWorkerAdvanceCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System);
        await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(new DecideWorkerAdvanceCommand(scenario.AdvanceId, scenario.AdvanceVersion, true, null, $"{_runId}-manager"), CancellationToken.None));
    }

    [Test]
    public async Task InstallmentSequenceAndApplicationTotalRemainExact()
    {
        var scenario = await CreateScenarioAsync(); await using var context = CreateContext(); var installments = await context.AdvanceInstallments.Where(item => item.WorkerAdvanceId == scenario.AdvanceId).OrderBy(item => item.Sequence).ToListAsync();
        installments.Select(item => item.Sequence).ShouldBe([1, 2, 3]); installments.Select(item => item.AmountUsd).ShouldBe([33.33m, 33.33m, 33.34m]); installments.Sum(item => item.AmountUsd).ShouldBe(100m);
    }

    [Test]
    public async Task IssueRetryIsIdempotent()
    {
        var scenario = await CreateScenarioAsync(true); await IssueAsync(scenario, "issue-retry", "0770001234"); await IssueAsync(scenario, "issue-retry", "0770001234");
        await using var verify = CreateContext(); (await verify.AdvanceIssues.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentApprovalCreatesOneAuthoritativeDecision()
    {
        var scenario = await CreateScenarioAsync(); using var barrier = new Barrier(2);
        var results = await Task.WhenAll(Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await DecideAsync(scenario, scenario.AdvanceVersion, true, "approval-a"); })), Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await DecideAsync(scenario, scenario.AdvanceVersion, false, "approval-b", "Synthetic rejection"); })));
        results.Count(value => value).ShouldBe(1); await using var verify = CreateContext(); (await verify.AdvanceApprovals.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentIssuanceCreatesOneAuthoritativeIssue()
    {
        var scenario = await CreateScenarioAsync(true); using var barrier = new Barrier(2);
        var results = await Task.WhenAll(Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await IssueAsync(scenario, "issue-a", "0770001234"); })), Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await IssueAsync(scenario, "issue-b", "0770001234"); })));
        results.Count(value => value).ShouldBe(1); await using var verify = CreateContext(); (await verify.AdvanceIssues.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1);
    }

    [Test]
    public async Task AdvanceApprovalUpdateIsRejected()
    { var scenario = await CreateScenarioAsync(); await DecideAsync(scenario, scenario.AdvanceVersion, true, "append-update"); await AssertAppendOnlyAsync("UPDATE payroll.\"AdvanceApprovals\" SET \"Reason\" = 'rewrite' WHERE \"WorkerAdvanceId\" = @id", scenario.AdvanceId); }

    [Test]
    public async Task AdvanceApprovalDeleteIsRejected()
    { var scenario = await CreateScenarioAsync(); await DecideAsync(scenario, scenario.AdvanceVersion, true, "append-delete"); await AssertAppendOnlyAsync("DELETE FROM payroll.\"AdvanceApprovals\" WHERE \"WorkerAdvanceId\" = @id", scenario.AdvanceId); }

    [Test]
    public async Task AdvanceIssueUpdateIsRejected()
    { var scenario = await CreateScenarioAsync(true); await IssueAsync(scenario, "issue-update", "0770001234"); await AssertAppendOnlyAsync("UPDATE payroll.\"AdvanceIssues\" SET \"TransactionStatus\" = 'rewrite' WHERE \"WorkerAdvanceId\" = @id", scenario.AdvanceId); }

    [Test]
    public async Task AdvanceIssueDeleteIsRejected()
    { var scenario = await CreateScenarioAsync(true); await IssueAsync(scenario, "issue-delete", "0770001234"); await AssertAppendOnlyAsync("DELETE FROM payroll.\"AdvanceIssues\" WHERE \"WorkerAdvanceId\" = @id", scenario.AdvanceId); }

    [Test]
    public async Task ApprovalIssueAndAuditFactsAreNotDuplicated()
    {
        var scenario = await CreateScenarioAsync(); await DecideAsync(scenario, scenario.AdvanceVersion, true, "audit-approval"); var approved = scenario with { AdvanceVersion = scenario.AdvanceVersion + 1 }; await IssueAsync(approved, "audit-issue", "0770001234"); await IssueAsync(approved, "audit-issue", "0770001234");
        await using var verify = CreateContext(); (await verify.AdvanceApprovals.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1); (await verify.AdvanceIssues.CountAsync(fact => fact.WorkerAdvanceId == scenario.AdvanceId)).ShouldBe(1); (await verify.PayrollAuditEventLinks.CountAsync(link => link.TenantId == scenario.TenantId && (link.AdvanceApprovalId != null || link.AdvanceIssueId != null))).ShouldBe(2);
    }

    [Test]
    public async Task MobileMoneyRecipientIsPersistedMasked()
    {
        var scenario = await CreateScenarioAsync(true); var result = await IssueAsync(scenario, "masked-recipient", "0770001234"); result.Issue!.MaskedRecipientNumber.ShouldBe("•••• 1234");
        await using var verify = CreateContext(); var stored = await verify.AdvanceIssues.Where(fact => fact.WorkerAdvanceId == scenario.AdvanceId).Select(fact => fact.MaskedRecipientNumber).SingleAsync(); stored.ShouldBe("•••• 1234"); stored!.ShouldNotContain("0770001234");
    }

    private async Task<Scenario> CreateScenarioAsync(bool approved = false)
    {
        var label = $"{_runId}-{Guid.NewGuid():N}"; var growerId = $"p6a-grower-{Guid.NewGuid():N}"; var managerId = $"p6a-manager-{Guid.NewGuid():N}"; var tenant = Tenant.CreateForGrower(growerId, label, null); var farm = tenant.CreateFarm($"P6A{Guid.NewGuid():N}"[..20], label, "Synthetic address", "Railway", "Synthetic", 10m, "Synthetic");
        var manager = farm.AddPerson("Synthetic manager", null, new DateOnly(2026, 1, 1)); farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1)); tenant.AddFarmManagerMembership(managerId, manager.Id); var workerPerson = farm.AddPerson("Synthetic worker", null, new DateOnly(2026, 1, 1));
        var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id, EmploymentType.Permanent, new DateOnly(2026, 1, 1), [1], new byte[12], new byte[16], "synthetic-key", Enumerable.Repeat((byte)1, 32).ToArray(), "***1234");
        var periods = Enumerable.Range(9, 3).Select(month => PayrollPeriod.Create(tenant.Id, farm.Id, 2026, month, DateTimeOffset.UtcNow, growerId, null)).ToArray(); var advance = WorkerAdvance.Create(tenant.Id, farm.Id, worker.Id, 100m, "Synthetic advance", new DateOnly(2026, 8, 27), periods[0].Id, 3, DateTimeOffset.UtcNow, managerId, manager.Id); advance.SetSchedule(periods.Select(period => period.Id).ToArray(), advance.Version); advance.Submit(advance.Version); var subjectVersion = advance.Version; AdvanceApproval? approval = null;
        if (approved) { approval = AdvanceApproval.Create(advance.Id, tenant.Id, farm.Id, subjectVersion, advance.RequestedAmountUsd, advance.Installments, true, growerId, DateTimeOffset.UtcNow, null, $"{label}-seed-approval"); advance.Decide(true, subjectVersion); }
        await using var context = CreateContext(); context.Users.AddRange(User(growerId), User(managerId)); context.Tenants.Add(tenant); context.WorkerProfiles.Add(worker); context.PayrollPeriods.AddRange(periods); context.WorkerAdvances.Add(advance); if (approval is not null) context.AdvanceApprovals.Add(approval); await context.SaveChangesAsync(); return new(tenant.Id, farm.Id, worker.Id, advance.Id, advance.Version, periods.Select(period => period.Id).ToArray(), growerId, managerId);
    }

    private async Task<WorkerAdvanceDto> DecideAsync(Scenario scenario, long version, bool approved, string key, string? reason = null)
    { await using var context = CreateContext(); return await new DecideWorkerAdvanceCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new DecideWorkerAdvanceCommand(scenario.AdvanceId, version, approved, reason, $"{_runId}-{key}"), CancellationToken.None); }

    private async Task<WorkerAdvanceDto> IssueAsync(Scenario scenario, string key, string recipient)
    { await using var context = CreateContext(); return await new IssueWorkerAdvanceCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.GrowerUserId)).Handle(new IssueWorkerAdvanceCommand(scenario.AdvanceId, scenario.AdvanceVersion, AdvancePaymentMethod.MobileMoney, 100m, DateTimeOffset.UtcNow, null, null, "Synthetic provider", recipient, $"{_runId}-{key}-reference", "Confirmed", $"{_runId}-{key}"), CancellationToken.None); }

    private async Task AssertAppendOnlyAsync(string sql, Guid id)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddWithValue("id", id); (await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState.ShouldBe(PostgresErrorCodes.RaiseException); }

    private async Task AssertSqlConstraintAsync(string sql, Guid id, Guid? other = null)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); command.Parameters.AddWithValue("id", id); if (other.HasValue) command.Parameters.AddWithValue("other", other.Value); (await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState.ShouldBeOneOf(PostgresErrorCodes.CheckViolation, PostgresErrorCodes.ForeignKeyViolation); }

    private static async Task<bool> AttemptAsync(Func<Task> action)
    { try { await action(); return true; } catch (ConflictException) { return false; } catch (ValidationException) { return false; } catch (DbUpdateException) { return false; } catch (PostgresException) { return false; } }

    private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString, options => options.CommandTimeout(120)).Options);
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"p6a-{Guid.NewGuid():N}"; }
    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid WorkerId, Guid AdvanceId, long AdvanceVersion, IReadOnlyList<Guid> PeriodIds, string GrowerUserId, string ManagerUserId);
}
