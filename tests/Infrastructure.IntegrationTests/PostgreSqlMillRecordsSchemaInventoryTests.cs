using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run after the approved Phase 7C migration is applied to Railway Development.")]
[Category("Phase7CSchemaInventory")]
[NonParallelizable]
public sealed class PostgreSqlMillRecordsSchemaInventoryTests
{
    [Test]
    public async Task LiveSchemaContainsCompletePhase7CInventory()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET")
            .ShouldBe("RailwayDevelopment");
        await using NpgsqlConnection connection = new(LoadConfiguredConnectionString());
        await connection.OpenAsync();

        string[] tables =
        [
            "Mills", "WeighbridgeTickets", "GrowerStatements", "EvidenceDocuments",
            "StatementTicketMatches", "MillRecordExports", "MillRecordAuditEventLinks"
        ];
        string[] functions =
        [
            "reject_authoritative_mutation", "protect_mill_reference",
            "protect_weighbridge_ticket", "protect_grower_statement",
            "validate_statement_ticket_match"
        ];
        string[] triggers =
        [
            "TR_Mills_NoDelete", "TR_WeighbridgeTickets_ProtectAuthoritative",
            "TR_GrowerStatements_ProtectAuthoritative",
            "TR_StatementTicketMatches_ValidateInsert",
            "TR_StatementTicketMatches_AppendOnly", "TR_EvidenceDocuments_AppendOnly",
            "TR_MillRecordExports_AppendOnly", "TR_MillRecordAuditEventLinks_AppendOnly"
        ];
        string[] indexes =
        [
            "IX_Mills_TenantId_FarmId_Code",
            "IX_WeighbridgeTickets_TenantId_FarmId_MillId_TicketReference",
            "IX_WeighbridgeTickets_TenantId_FarmId_RecordingIdempotencyKey",
            "IX_GrowerStatements_TenantId_FarmId_MillId_StatementReference",
            "IX_GrowerStatements_TenantId_FarmId_RecordingIdempotencyKey",
            "IX_StatementTicketMatches_TenantId_FarmId_IdempotencyKey"
        ];

        (await CountAsync(connection,
            "SELECT count(*) FROM information_schema.tables WHERE table_schema='mill' AND table_name=ANY(@names)",
            tables)).ShouldBe(tables.Length);
        (await CountAsync(connection,
            "SELECT count(DISTINCT p.proname) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='mill' AND p.proname=ANY(@names)",
            functions)).ShouldBe(functions.Length);
        (await CountAsync(connection,
            "SELECT count(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='mill' AND NOT t.tgisinternal AND t.tgname=ANY(@names)",
            triggers)).ShouldBe(triggers.Length);
        (await CountAsync(connection,
            "SELECT count(*) FROM pg_indexes WHERE schemaname='mill' AND indexname=ANY(@names)",
            indexes)).ShouldBe(indexes.Length);

        (await CountAsync(connection,
            "SELECT count(*) FROM pg_constraint c WHERE c.contype='f' AND c.conrelid='mill.\"EvidenceDocuments\"'::regclass AND c.confrelid IN ('mill.\"WeighbridgeTickets\"'::regclass, 'mill.\"GrowerStatements\"'::regclass)",
            null)).ShouldBe(2);
        (await CountAsync(connection,
            "SELECT count(*) FROM pg_constraint c WHERE c.contype='f' AND c.conrelid='mill.\"MillRecordAuditEventLinks\"'::regclass",
            null)).ShouldBe(7);

        TestContext.Progress.WriteLine(
            $"tables={tables.Length}; functions={functions.Length}; triggers={triggers.Length}; required indexes={indexes.Length}; evidence typed FKs=2; audit typed FKs=7");
    }

    private static async Task<int> CountAsync(NpgsqlConnection connection, string sql,
        string[]? names)
    {
        await using NpgsqlCommand command = new(sql, connection);
        if (names is not null) command.Parameters.AddWithValue("names", names);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static string LoadConfiguredConnectionString()
    {
        string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db");
        if (!string.IsNullOrWhiteSpace(value)) return value;
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException(
            "The configured Railway development connection is unavailable.");
    }
}
