using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Creates a labelled legacy profile before CR-01.1 migration; retained for acceptance.")]
[Category("CR01Prerequisite")]
public sealed class PostgreSqlFarmOwnerProfilePrerequisiteTests
{
    [Test]
    public async Task CreateSyntheticLegacyOwnerUsingExistingColumnsOnly()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        string label = Environment.GetEnvironmentVariable("CANE360_CR01_LEGACY_LABEL")
            ?? throw new InvalidOperationException("A unique synthetic label is required.");
        label.ShouldStartWith("AUTOTEST-CR01-LEGACY-");
        label.Length.ShouldBeLessThanOrEqualTo(120);
        var configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables().Build();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Cane360Db")).Options);
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        Guid tenantId = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();
        string tenantCode = $"CR01-{tenantId:N}"[..24];
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO identity."Tenants" ("Id", "TenantCode", "Status", "Created", "LastModified")
            VALUES ({tenantId}, {tenantCode}, 'Active', {now}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO identity."GrowerProfiles" ("Id", "TenantId", "DisplayName", "Phone", "Created", "LastModified")
            VALUES ({profileId}, {tenantId}, {label}, NULL, {now}, {now})
            """);
        await transaction.CommitAsync();
    }
}
