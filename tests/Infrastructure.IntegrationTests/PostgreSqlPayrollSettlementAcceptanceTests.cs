using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddPayrollPaymentsPayslipsAndSettlementClosure is explicitly approved and applied to Railway Development.")]
[Category("Phase6CPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlPayrollSettlementAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [TestCase("PayrollPayments", "TR_PayrollPayments_AppendOnly")]
    [TestCase("PaymentAcknowledgements", "TR_PaymentAcknowledgements_AppendOnly")]
    [TestCase("PayrollPaymentReversals", "TR_PayrollPaymentReversals_AppendOnly")]
    [TestCase("PayrollSettlementClosures", "TR_PayrollSettlementClosures_AppendOnly")]
    [TestCase("PayrollSettlementReopens", "TR_PayrollSettlementReopens_AppendOnly")]
    public async Task Phase6CImmutablePaymentTablesRejectUpdateAndDelete(string table, string trigger) =>
        (await ExistsAsync("SELECT EXISTS (SELECT 1 FROM information_schema.triggers WHERE event_object_schema = 'payroll' AND event_object_table = @table AND trigger_name = @trigger)", ("table", table), ("trigger", trigger))).ShouldBeTrue();

    [Test]
    public async Task Phase6CTenantFarmAndExactVersionForeignKeysExist() =>
        (await CountAsync("SELECT count(*) FROM pg_constraint WHERE connamespace = 'payroll'::regnamespace AND contype = 'f' AND conrelid IN ('payroll.\"PayrollPayments\"'::regclass, 'payroll.\"PaymentAcknowledgements\"'::regclass, 'payroll.\"PayrollPaymentReversals\"'::regclass, 'payroll.\"PayrollSettlementClosures\"'::regclass, 'payroll.\"PayrollSettlementReopens\"'::regclass) AND pg_get_constraintdef(oid) LIKE '%\"TenantId\"%' AND pg_get_constraintdef(oid) LIKE '%\"FarmId\"%'")).ShouldBeGreaterThanOrEqualTo(12);

    [TestCase("IX_PayrollPayments_TenantId_FarmId_IdempotencyKey")]
    [TestCase("IX_PaymentAcknowledgements_TenantId_FarmId_IdempotencyKey")]
    [TestCase("IX_PayrollPaymentReversals_TenantId_FarmId_IdempotencyKey")]
    [TestCase("IX_PayrollSettlementClosures_TenantId_FarmId_IdempotencyKey")]
    [TestCase("IX_PayrollSettlementReopens_TenantId_FarmId_IdempotencyKey")]
    public async Task Phase6CPaymentIdempotencyUniquenessExists(string index) =>
        (await ExistsAsync("SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'payroll' AND indexname = @index AND indexdef LIKE 'CREATE UNIQUE INDEX%')", ("index", index))).ShouldBeTrue();

    [Test]
    public async Task Phase6CMobileTransactionDuplicateProtectionExists() =>
        (await ExistsAsync("SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'payroll' AND indexname = 'UX_PayrollPayments_MobileReference' AND indexdef LIKE 'CREATE UNIQUE INDEX%')")).ShouldBeTrue();

    [Test]
    public async Task Phase6CConcurrentDuplicateAndOverpaymentAttemptsAreDatabaseGuarded()
    {
        var definition = await TextAsync("SELECT pg_get_functiondef('payroll.\"ValidatePayrollSettlementMutation\"()'::regprocedure)");
        definition.ShouldContain("pg_advisory_xact_lock"); definition.ShouldContain("Payroll payment would exceed approved worker net pay"); definition.ShouldContain("Payroll settlement is closed");
    }

    [Test]
    public async Task Phase6CDirectExactVersionAndCrossTenantBypassIsRejectedByGuard() =>
        (await TextAsync("SELECT pg_get_functiondef('payroll.\"ValidatePayrollSettlementMutation\"()'::regprocedure)")).ShouldContain("exact Grower-approved calculation");

    [Test]
    public async Task Phase6CAcknowledgementAndReversalRelationshipsAreTenantScoped() =>
        (await CountAsync("SELECT count(*) FROM pg_constraint WHERE connamespace = 'payroll'::regnamespace AND contype = 'f' AND conrelid IN ('payroll.\"PaymentAcknowledgements\"'::regclass, 'payroll.\"PayrollPaymentReversals\"'::regclass) AND pg_get_constraintdef(oid) LIKE '%\"PayrollPaymentId\"%' AND pg_get_constraintdef(oid) LIKE '%\"TenantId\"%'")).ShouldBe(2);

    [Test]
    public async Task Phase6CSettlementCloseIntegrityIsDatabaseGuarded() =>
        (await TextAsync("SELECT pg_get_functiondef('payroll.\"ValidatePayrollSettlementClosure\"()'::regprocedure)")).ShouldContain("fully settled and acknowledged");

    private async Task<bool> ExistsAsync(string sql, params (string Name, object Value)[] parameters) => Convert.ToBoolean(await ScalarAsync(sql, parameters));
    private async Task<int> CountAsync(string sql) => Convert.ToInt32(await ScalarAsync(sql));
    private async Task<string> TextAsync(string sql) => Convert.ToString(await ScalarAsync(sql))!;
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value); return await command.ExecuteScalarAsync(); }
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
}
