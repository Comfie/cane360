using Cane360.Application.Common.Interfaces;
using Cane360.Application.FarmSetup;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Cane360.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only against Railway Development after CR-01.1 migration is applied.")]
[Category("CR01PostMigration")]
[NonParallelizable]
public sealed class PostgreSqlFarmOwnerProfileAcceptanceTests
{
    private IConfiguration _configuration = null!;
    private WorkerSensitiveDataProtector _protector = null!;
    private string _userId = string.Empty;
    private string _runId = string.Empty;
    private Guid _tenantId;
    private Guid _foreignTenantId;
    private Guid _modelId;

    [OneTimeSetUp]
    public async Task CreateLabelledSyntheticTenants()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cane360.slnx")))
            directory = directory.Parent;
        string localSettings = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable."),
            "src", "Web", "appsettings.Development.Local.json");
        _configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables().Build();
        var local = new ConfigurationBuilder().AddJsonFile(localSettings, optional: true).Build();
        _protector = new WorkerSensitiveDataProtector(new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddInMemoryCollection(local.GetSection("Cane360Security:NationalId").AsEnumerable()).Build());
        _runId = $"AUTOTEST-CR01-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _userId = $"cr01-{Guid.NewGuid():N}";
        string foreignUser = $"cr01-{Guid.NewGuid():N}";
        Tenant tenant = Tenant.CreateForGrower(_userId, _runId, null);
        Farm farm = tenant.CreateFarm("SYNTHETIC", _runId, "Synthetic", "Synthetic", "Owned", 10, "Synthetic");
        Field field = farm.AddField("TEST", "Synthetic field", 10, null, ReportingAreaSource.Declared, "Synthetic", null);
        CropVariety variety = tenant.AddCropVariety("SYNTHETIC", "Synthetic variety");
        field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, "Synthetic variety", new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), new DateOnly(2027, 2, 1), 100, DateTimeOffset.UtcNow, _userId);
        Tenant foreign = Tenant.CreateForGrower(foreignUser, _runId + "-foreign", null);
        foreign.CreateFarm("SYNTHETIC", _runId + "-foreign", "Synthetic", "Synthetic", "Owned", 10, "Synthetic");
        _tenantId = tenant.Id; _foreignTenantId = foreign.Id;
        FarmModel model = FarmModel.Create(tenant.Id, "SYNTHETIC", "Synthetic model");
        _modelId = model.Id;
        await using var context = Context();
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        context.Users.AddRange(User(_userId), User(foreignUser));
        context.Tenants.AddRange(tenant, foreign);
        context.FarmModels.Add(model);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task PreMigrationSyntheticOwnerSurvivesWithNullNewFields()
    {
        string label = Environment.GetEnvironmentVariable("CANE360_CR01_LEGACY_LABEL")
            ?? throw new InvalidOperationException("Supply the uniquely labelled pre-migration synthetic owner.");
        label.ShouldStartWith("AUTOTEST-CR01-LEGACY-");
        await using var context = Context();
        GrowerProfile profile = await context.GrowerProfiles.SingleAsync(item => item.DisplayName == label &&
            context.Tenants.Any(tenant => tenant.Id == item.TenantId && tenant.TenantCode.StartsWith("CR01-")));
        profile.FirstName.ShouldBeNull();
        profile.NationalIdCiphertext.ShouldBeNull();
        profile.Active.ShouldBeTrue();
    }

    [Test]
    public async Task ProfilePersistenceProtectsIdentityAndLoadsExistingFieldsAndCycles()
    {
        await using var context = Context();
        var repository = new FarmSetupRepository(context);
        var user = new SyntheticUser(_userId, _runId);
        var handler = new UpdateFarmInformationCommandHandler(repository, user,
            _protector, TimeProvider.System);
        FarmSetupDto result = await handler.Handle(new UpdateFarmInformationCommand(_runId, "+263000000000",
            "SYNTHETIC", _runId, "Synthetic", "Synthetic", "Owned", 10, "Synthetic",
            new FarmOwnerProfileInput("Ms", "Synthetic", "Owner", "Female", "SYN-G1", "Synthetic association",
                "SYN-M1", "Synthetic address", "synthetic@example.invalid", "asset:synthetic-photo", true,
                "63-123456-A-12"), _modelId, true), CancellationToken.None);
        result.Grower!.NationalIdMask.ShouldBe("••••••12");
        await using var verify = Context();
        GrowerProfile stored = await verify.GrowerProfiles.SingleAsync(item => item.TenantId == _tenantId);
        stored.Association.ShouldBe("Synthetic association");
        stored.GrowerNumber.ShouldBe("SYN-G1");
        stored.NationalIdCiphertext.ShouldNotBe(System.Text.Encoding.UTF8.GetBytes("63123456A12"));
        var reveal = await new RevealFarmOwnerNationalIdCommandHandler(repository, user,
            _protector, TimeProvider.System)
            .Handle(new RevealFarmOwnerNationalIdCommand(), CancellationToken.None);
        reveal.NationalId.ShouldBe("63123456A12");
        (await verify.AuditEvents.Where(item => item.TenantId == _tenantId && item.CorrelationId == _runId)
            .Select(item => item.Action).ToListAsync()).ShouldContain("NationalIdRevealSucceeded");
        Tenant loaded = (await repository.GetTenantForUserAsync(_userId, false, CancellationToken.None))!;
        loaded.ActiveFarm!.FarmModelId.ShouldBe(_modelId);
        loaded.ActiveFarm.Fields.Single().CropCycles.Count.ShouldBe(1);
    }

    [Test]
    public async Task PostgreSqlRejectsCrossTenantModelReference()
    {
        await using var context = Context();
        await using var transaction = await context.Database.BeginTransactionAsync();
        Farm farm = await context.Farms.SingleAsync(item => item.TenantId == _foreignTenantId);
        context.Entry(farm).Property(item => item.FarmModelId).CurrentValue = _modelId;
        DbUpdateException error = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
        ((PostgresException)error.InnerException!).SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task PostgreSqlRejectsDuplicateModelCodeWithinSyntheticTenant()
    {
        await using var context = Context();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.FarmModels.Add(FarmModel.Create(_tenantId, "synthetic", "Duplicate synthetic"));
        DbUpdateException error = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
        ((PostgresException)error.InnerException!).ConstraintName.ShouldBe("IX_FarmModels_TenantId_Code");
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task FarmOwnerAndModelQueriesRemainTenantScoped()
    {
        await using var context = Context();
        var repository = new FarmSetupRepository(context);
        var own = await repository.GetFarmModelsAsync(_tenantId, false, CancellationToken.None);
        own.ShouldAllBe(item => item.TenantId == _tenantId);
        (await repository.GetFarmModelsAsync(_foreignTenantId, false, CancellationToken.None)).ShouldBeEmpty();
        (await repository.GetTenantWorkspaceForUserAsync($"unlinked-{Guid.NewGuid():N}", CancellationToken.None)).ShouldBeNull();
        (await repository.GetTenantWorkspaceForUserAsync(_userId, CancellationToken.None))!.Id.ShouldBe(_tenantId);
    }

    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(_configuration.GetConnectionString("Cane360Db")).Options);

    private static ApplicationUser User(string id) => new()
    {
        Id = id, UserName = $"{id}@example.invalid", NormalizedUserName = $"{id}@EXAMPLE.INVALID".ToUpperInvariant(),
        Email = $"{id}@example.invalid", NormalizedEmail = $"{id}@EXAMPLE.INVALID".ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };

    private sealed class SyntheticUser(string id, string reference) : IUser
    {
        public List<string>? Roles => [];
        public string? Id => id;
        public string? CorrelationId => reference;
    }
}
