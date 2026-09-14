using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run before Phase 7C migration approval to verify reused Railway PostgreSQL functions.")]
[Category("Phase7CPreMigration")]
[NonParallelizable]
public sealed class PostgreSqlMillRecordsPrerequisiteTests
{
    [Test]
    public async Task ReusedSchemaQualifiedPostgreSqlFunctionsExist()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET")
            .ShouldBe("RailwayDevelopment");
        await using NpgsqlConnection connection = new(LoadConfiguredConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("SELECT to_regprocedure('pg_catalog.pg_advisory_xact_lock(bigint)') IS NOT NULL, to_regprocedure('pg_catalog.hashtextextended(text,bigint)') IS NOT NULL", connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        reader.GetBoolean(0).ShouldBeTrue("pg_catalog.pg_advisory_xact_lock(bigint) is required");
        reader.GetBoolean(1).ShouldBeTrue("pg_catalog.hashtextextended(text,bigint) is required");
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
