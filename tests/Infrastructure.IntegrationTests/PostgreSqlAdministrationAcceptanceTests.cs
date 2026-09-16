using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddAdministrationConfiguration is approved and applied to Railway Development.")]
[Category("Phase8BPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlAdministrationAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;
    private string _userId = string.Empty;
    private Guid _tenantId;
    private Guid _farmId;
    private Guid _activityTypeId;

    [OneTimeSetUp]
    public async Task EstablishSyntheticTenant()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET")
            .ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
        _runId = $"AUTOTEST-P8B-ADMIN-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _userId = $"p8b-admin-{Guid.NewGuid():N}";
        Tenant tenant = Tenant.CreateForGrower(_userId, _runId, null);
        Farm farm = tenant.CreateFarm($"F{Guid.NewGuid():N}"[..20], _runId,
            "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic");
        var type = tenant.AddActivityType($"A{Guid.NewGuid():N}"[..20],
            "Synthetic administration work", true, true, ActivityQuantityBasis.Hectares);
        _tenantId = tenant.Id;
        _farmId = farm.Id;
        _activityTypeId = type.Id;
        await using var context = CreateContext();
        context.Users.Add(User(_userId));
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        TestContext.Progress.WriteLine($"Retained synthetic Administration run: {_runId}; tenant: {_tenantId}");
    }

    [Test]
    public async Task ActivityTypeNormalizedUniquenessIsEnforced()
    {
        await using var context = CreateContext();
        var source = await context.ActivityTypes.AsNoTracking().SingleAsync(item =>
            item.Id == _activityTypeId && item.TenantId == _tenantId);
        var duplicate = await context.Tenants.Include(item => item.ActivityTypes)
            .SingleAsync(item => item.Id == _tenantId);
        Should.Throw<InvalidOperationException>(() => duplicate.AddActivityType(
            source.Code.ToLowerInvariant(), "Duplicate", true, true, ActivityQuantityBasis.Hectares));
    }

    [Test]
    public async Task DocumentCategoryCodeIsTenantUniqueInPostgreSql()
    {
        string code = $"C{Guid.NewGuid():N}"[..20];
        await using (var first = CreateContext())
        {
            first.DocumentCategories.Add(DocumentCategory.Create(_tenantId, code,
                $"{_runId} category", null));
            await first.SaveChangesAsync();
        }
        await using var second = CreateContext();
        second.DocumentCategories.Add(DocumentCategory.Create(_tenantId, code.ToLowerInvariant(),
            "Duplicate", null));
        DbUpdateException error = await Should.ThrowAsync<DbUpdateException>(
            () => second.SaveChangesAsync());
        ((PostgresException)error.InnerException!).ConstraintName
            .ShouldBe("IX_DocumentCategories_TenantId_Code");
        await using var verify = CreateContext();
        (await verify.DocumentCategories.CountAsync(item => item.TenantId == _tenantId &&
            item.Code == code.ToUpperInvariant())).ShouldBe(1);
    }

    [Test]
    public async Task FarmSettingEffectiveRangesCannotOverlapInPostgreSql()
    {
        await using (var first = CreateContext())
        {
            first.FarmSettings.Add(FarmSetting.Create(_tenantId, _farmId,
                FarmSetting.ActivityLateEntryReasonDays, 2,
                new DateOnly(2050, 1, 1), new DateOnly(2050, 12, 31)));
            await first.SaveChangesAsync();
        }
        await using var second = CreateContext();
        second.FarmSettings.Add(FarmSetting.Create(_tenantId, _farmId,
            FarmSetting.ActivityLateEntryReasonDays, 3,
            new DateOnly(2050, 6, 1), new DateOnly(2051, 1, 1)));
        DbUpdateException error = await Should.ThrowAsync<DbUpdateException>(
            () => second.SaveChangesAsync());
        ((PostgresException)error.InnerException!).ConstraintName.ShouldBe("EX_FarmSettings_NoOverlap");
    }

    [Test]
    public async Task ConcurrentSettingCreationHasOneFinalEffectiveVersion()
    {
        DateOnly from = new(2060, 1, 1);
        async Task<Exception?> InsertAsync(int value)
        {
            try
            {
                await using var context = CreateContext();
                context.FarmSettings.Add(FarmSetting.Create(_tenantId, _farmId,
                    FarmSetting.ActivityLateEntryReasonDays, value, from, null));
                await context.SaveChangesAsync();
                return null;
            }
            catch (Exception exception) { return exception; }
        }

        Exception?[] results = await Task.WhenAll(InsertAsync(2), InsertAsync(3));
        await using var verify = CreateContext();
        int finalCount = await verify.FarmSettings.CountAsync(item => item.TenantId == _tenantId &&
            item.FarmId == _farmId && item.Key == FarmSetting.ActivityLateEntryReasonDays &&
            item.EffectiveFrom == from);
        finalCount.ShouldBe(1);
        results.Count(item => item is null).ShouldBe(1);
        results.Count(item => item is DbUpdateException
            { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation } })
            .ShouldBe(1);
    }

    [Test]
    public async Task AuditFactsCannotBeMutated()
    {
        AuditEvent audit = AuditEvent.Create(_tenantId, _farmId, "Administration",
            Guid.NewGuid(), "Created", _userId, TenantSecurityRoles.Grower, null,
            DateTimeOffset.UtcNow, $"{_runId}-audit", null,
            "Synthetic Administration audit fact.");
        await using (var first = CreateContext())
        {
            first.AuditEvents.Add(audit);
            await first.SaveChangesAsync();
        }
        await using var second = CreateContext();
        PostgresException error = await Should.ThrowAsync<PostgresException>(async () =>
            await second.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE audit.\"AuditEvents\" SET \"SafeSummary\"='tampered' WHERE \"Id\"={audit.Id} AND \"TenantId\"={_tenantId}"));
        error.SqlState.ShouldBe("P0001");
    }

    private ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);

    private static string LoadConfiguredConnectionString()
    {
        string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db");
        if (!string.IsNullOrWhiteSpace(value)) return value;
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        return configuration.GetConnectionString("Cane360Db")
            ?? throw new InvalidOperationException("Railway Development connection is unavailable.");
    }

    private static ApplicationUser User(string id) => new()
    {
        Id = id,
        UserName = $"{id}@invalid.example",
        NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(),
        Email = $"{id}@invalid.example",
        NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };
}
