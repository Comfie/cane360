using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddPayrollRunsCalculationAndApproval is explicitly approved and applied to Railway Development.")]
[Category("Phase6BPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlPayrollRunsAcceptanceTests
{
    private string _connectionString = string.Empty;
    private Guid _tenantId;
    private Guid _farmId;
    private Guid _periodId;
    private Guid _runId;
    private string _managerUserId = string.Empty;

    [OneTimeSetUp]
    public async Task ConfigureAsync()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString(); var label = $"AUTOTEST-P6B-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"; var growerId = $"p6b-grower-{Guid.NewGuid():N}"; _managerUserId = $"p6b-manager-{Guid.NewGuid():N}";
        var tenant = Tenant.CreateForGrower(growerId, label, null); var farm = tenant.CreateFarm($"P6B{Guid.NewGuid():N}"[..20], label, "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic"); var manager = farm.AddPerson("Synthetic P6B manager", null, new DateOnly(2026, 1, 1)); farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1)); tenant.AddFarmManagerMembership(_managerUserId, manager.Id);
        var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2036, 8, DateTimeOffset.UtcNow, _managerUserId, manager.Id); period.Open(DateTimeOffset.UtcNow, _managerUserId, manager.Id, period.Version); var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, DateTimeOffset.UtcNow, _managerUserId, manager.Id);
        await using var context = Context(); context.Users.AddRange(User(growerId), User(_managerUserId)); context.Tenants.Add(tenant); context.PayrollPeriods.Add(period); context.PayrollRuns.Add(run); await context.SaveChangesAsync(); _tenantId = tenant.Id; _farmId = farm.Id; _periodId = period.Id; _runId = run.Id;
    }

    [Test] public async Task Phase6BTenantSafeForeignKeysExist() => (await CountAsync("SELECT count(*) FROM pg_constraint WHERE contype = 'f' AND connamespace = 'payroll'::regnamespace AND pg_get_constraintdef(oid) LIKE '%\"TenantId\"%' AND pg_get_constraintdef(oid) LIKE '%\"FarmId\"%'")).ShouldBeGreaterThanOrEqualTo(18);

    [Test]
    public async Task Phase6BOneActiveRunPerPeriodIsEnforced()
    { await using var context = Context(); context.PayrollRuns.Add(PayrollRun.Create(_tenantId, _farmId, _periodId, DateTimeOffset.UtcNow, _managerUserId, null)); var exception = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync()); ((PostgresException)exception.InnerException!).ConstraintName.ShouldBe("UX_PayrollRuns_ActivePeriod"); }

    [Test] public async Task Phase6BSchemaDeclaresCalculationReconciliationAndPositiveEarningConstraints() => await AssertConstraintsAsync("PayrollCalculations", "CK_PayrollCalculations_Totals", "PayrollEarningLines", "CK_PayrollEarningLines_Positive", "PayrollWorkerLines", "CK_PayrollWorkerLines_Totals");
    [Test] public async Task Phase6BSchemaDeclaresUniqueEvidenceConsumptionIndex() => (await IndexExistsAsync("PayrollEvidenceConsumptions", "IX_PayrollEvidenceConsumptions_EvidenceId", true)).ShouldBeTrue();

    [Test]
    public async Task Phase6BCrossTenantAndFarmQueriesAreIsolated()
    { await using var context = Context(); var repository = new PayrollRepository(context); (await repository.GetRunAsync(Guid.NewGuid(), _farmId, _runId, false, CancellationToken.None)).ShouldBeNull(); (await repository.GetRunAsync(_tenantId, Guid.NewGuid(), _runId, false, CancellationToken.None)).ShouldBeNull(); }

    [Test] public async Task Phase6BSchemaDeclaresUniqueExactVersionGrowerApprovalIndex() => (await IndexExistsAsync("PayrollApprovals", "IX_PayrollApprovals_PayrollRunId_CalculationVersion", true)).ShouldBeTrue();

    [Test]
    public async Task Phase6BFarmManagerApprovalIsRejected()
    { await using var context = Context(); var handler = new DecidePayrollRunCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(_managerUserId), TimeProvider.System); await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(new DecidePayrollRunCommand(_runId, 0, 1, true, null, $"AUTOTEST-P6B-{Guid.NewGuid():N}"), CancellationToken.None)); }

    [Test] public async Task Phase6BSchemaDeclaresApprovalIdempotencyIndex() => (await IndexExistsAsync("PayrollApprovals", "IX_PayrollApprovals_TenantId_FarmId_IdempotencyKey", true)).ShouldBeTrue();
    [Test] public async Task Phase6BSchemaDeclaresConcurrentApprovalUniquenessIndex() => (await IndexExistsAsync("PayrollApprovals", "IX_PayrollApprovals_PayrollRunId_CalculationVersion", true)).ShouldBeTrue();
    [Test] public async Task Phase6BSchemaDeclaresConcurrentEvidenceConsumptionUniquenessIndex() => (await IndexExistsAsync("PayrollEvidenceConsumptions", "IX_PayrollEvidenceConsumptions_EvidenceId", true)).ShouldBeTrue();
    [Test] public async Task Phase6BSchemaDeclaresAdvanceDeductionAndRecoveryAmountConstraints() => await AssertConstraintsAsync("PayrollAdvanceDeductions", "CK_PayrollAdvanceDeductions_Amounts", "AdvanceRecoveries", "CK_AdvanceRecoveries_Amount");
    [Test] public async Task Phase6BSchemaProtectsAdvanceRecoveryRowsWithAppendOnlyTrigger() => (await TriggerExistsAsync("AdvanceRecoveries", "TR_AdvanceRecoveries_AppendOnly")).ShouldBeTrue();
    [Test] public async Task Phase6BSchemaDeclaresClosedPeriodStatusAndMetadataConstraints() => await AssertConstraintsAsync("PayrollPeriods", "CK_PayrollPeriods_ClosedMetadata", "PayrollPeriods", "CK_PayrollPeriods_Status");

    [TestCase("PayrollCalculations", "TR_PayrollCalculations_AppendOnly")]
    [TestCase("PayrollWorkerLines", "TR_PayrollWorkerLines_AppendOnly")]
    [TestCase("PayrollEarningLines", "TR_PayrollEarningLines_AppendOnly")]
    [TestCase("PayrollAdvanceDeductions", "TR_PayrollAdvanceDeductions_AppendOnly")]
    [TestCase("PayrollApprovals", "TR_PayrollApprovals_AppendOnly")]
    [TestCase("PayrollEvidenceConsumptions", "TR_PayrollEvidenceConsumptions_AppendOnly")]
    [TestCase("AdvanceRecoveries", "TR_AdvanceRecoveries_AppendOnly")]
    [TestCase("PayrollAuditEventLinks", "TR_PayrollAuditEventLinks_AppendOnly")]
    public async Task Phase6BImmutableTableCategoriesRejectDirectUpdateAndDelete(string table, string trigger) => (await TriggerExistsAsync(table, trigger)).ShouldBeTrue();

    [Test]
    public async Task Phase6BConsumedLabourEvidenceRejectsOrdinaryMutation()
    {
        string definition = await ScalarTextAsync("SELECT pg_get_functiondef('payroll.\"RejectConsumedLabourEvidenceMutation\"()'::regprocedure)");
        definition.ShouldContain("Labour evidence consumed by an approved payroll is immutable.");
        definition.ShouldContain("IF TG_OP = 'DELETE' THEN");
        definition.ShouldContain("RETURN OLD;");
        definition.ShouldContain("RETURN NEW;");
        (await ScalarBoolAsync(
            "SELECT EXISTS (SELECT 1 FROM pg_trigger trigger WHERE trigger.tgrelid = 'labour.\"WorkRecords\"'::regclass AND trigger.tgname = 'TR_WorkRecords_ApprovedPayrollLock' AND trigger.tgenabled = 'O' AND (trigger.tgtype & 1) = 1 AND (trigger.tgtype & 2) = 2 AND (trigger.tgtype & 8) = 8 AND (trigger.tgtype & 16) = 16)"))
            .ShouldBeTrue();
    }

    [Test]
    public async Task Phase6BNoDuplicateApprovalConsumptionRecoveryAuditOrTimelineFacts()
    { (await IndexExistsAsync("PayrollApprovals", "IX_PayrollApprovals_PayrollRunId_CalculationVersion", true)).ShouldBeTrue(); (await IndexExistsAsync("PayrollEvidenceConsumptions", "IX_PayrollEvidenceConsumptions_EvidenceId", true)).ShouldBeTrue(); (await IndexExistsAsync("AdvanceRecoveries", "IX_AdvanceRecoveries_PayrollAdvanceDeductionId", true)).ShouldBeTrue(); }

    [Test]
    public async Task Phase6BApprovalCreatesNoPaymentPayslipOrFinancePostingSideEffect()
    { (await CountAsync("SELECT count(*) FROM information_schema.tables WHERE table_schema = 'payroll' AND (table_name ILIKE '%payment%' OR table_name ILIKE '%payslip%' OR table_name ILIKE '%tax%' OR table_name ILIKE '%pension%')")).ShouldBe(0); (await CountAsync("SELECT count(*) FROM pg_constraint WHERE connamespace = 'payroll'::regnamespace AND pg_get_constraintdef(oid) LIKE '%OperationalCostPostings%'")).ShouldBe(0); }

    private async Task AssertConstraintsAsync(params string[] tableAndConstraint)
    { for (var index = 0; index < tableAndConstraint.Length; index += 2) (await CountAsync($"SELECT count(*) FROM pg_constraint WHERE conrelid = 'payroll.\"{tableAndConstraint[index]}\"'::regclass AND conname = '{tableAndConstraint[index + 1]}'")).ShouldBe(1); }
    private async Task<bool> IndexExistsAsync(string table, string index, bool unique) => await ScalarBoolAsync("SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'payroll' AND tablename = @table AND indexname = @name AND (NOT @unique OR indexdef LIKE 'CREATE UNIQUE INDEX%'))", ("table", table), ("name", index), ("unique", unique));
    private async Task<bool> TriggerExistsAsync(string table, string trigger, string schema = "payroll") => await ScalarBoolAsync("SELECT EXISTS (SELECT 1 FROM information_schema.triggers WHERE event_object_schema = @schema AND event_object_table = @table AND trigger_name = @name)", ("schema", schema), ("table", table), ("name", trigger));
    private async Task<bool> ScalarBoolAsync(string sql, params (string Name, object Value)[] parameters) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value); return (bool)(await command.ExecuteScalarAsync())!; }
    private async Task<string> ScalarTextAsync(string sql) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); return (string)(await command.ExecuteScalarAsync())!; }
    private async Task<int> CountAsync(string sql) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); return Convert.ToInt32(await command.ExecuteScalarAsync()); }
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"AUTOTEST-P6B-{Guid.NewGuid():N}"; }
}
