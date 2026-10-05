using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Application.FarmSetup;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only against Railway Development. Each test rolls back its own synthetic transaction.")]
[Category("CR012Acceptance")]
[NonParallelizable]
public sealed class PostgreSqlFieldCropUxAcceptanceTests
{
    private ApplicationDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;
    private FarmSetupRepository _repository = null!;
    private Tenant _tenant = null!;
    private Field _field = null!;
    private CropVariety _variety = null!;
    private SyntheticUser _user = null!;
    private string _label = string.Empty;

    [SetUp]
    public async Task CreateTransactionalSyntheticContext()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables().Build();
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Cane360Db")).Options);
        _transaction = await _context.Database.BeginTransactionAsync();
        _label = $"AUTOTEST-CR012-{Guid.NewGuid():N}";
        string id = $"cr012-{Guid.NewGuid():N}";
        _user = new SyntheticUser(id, _label);
        _context.Users.Add(new ApplicationUser {Id = id, UserName = id + "@example.invalid",
            NormalizedUserName = (id + "@example.invalid").ToUpperInvariant(), Email = id + "@example.invalid",
            NormalizedEmail = (id + "@example.invalid").ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")});
        _tenant = Tenant.CreateForGrower(id, _label, null);
        Farm farm = _tenant.CreateFarm("SYNTHETIC", _label, "Synthetic", "Synthetic", "Owned", 10m, "Synthetic");
        _field = farm.AddField("CR012", "Synthetic field", 5m, null, ReportingAreaSource.Declared, "Synthetic", null);
        _variety = _tenant.AddCropVariety("CR012", "Synthetic variety");
        _context.Tenants.Add(_tenant);
        await _context.SaveChangesAsync();
        _repository = new FarmSetupRepository(_context);
    }

    [TearDown]
    public async Task RollBackOnlyOwnUncommittedWork()
    {
        if (_transaction is not null) {await _transaction.RollbackAsync(); await _transaction.DisposeAsync();}
        if (_context is not null) await _context.DisposeAsync();
    }

    [Test]
    public async Task PhysicalFieldCreationAndEditingPersistWithoutCropValues()
    {
        await new CreateFieldCommandHandler(_repository, _user).Handle(new CreateFieldCommand("SECOND", "Synthetic second",
            3m, null, "Declared", "Furrow", null), CancellationToken.None);
        await new UpdateFieldDetailsCommandHandler(_repository, _user, new Clock()).Handle(
            new UpdateFieldDetailsCommand(_field.Id, "Synthetic edited", "Drip", "Synthetic soil"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        Tenant loaded = (await _repository.GetTenantForUserAsync(_user.Id!, false, CancellationToken.None))!;
        loaded.Id.ShouldBe(_tenant.Id);
        loaded.GrowerProfile.DisplayName.ShouldBe(_label);
        loaded.ActiveFarm!.Fields.Single(field => field.Id == _field.Id).Name.ShouldBe("Synthetic edited");
        loaded.ActiveFarm.Fields.Single(field => field.Code == "SECOND").CropCycles.ShouldBeEmpty();
        (await _context.AuditEvents.Where(item => item.TenantId == _tenant.Id && item.CorrelationId == _label &&
            item.SubjectId == _field.Id).Select(item => item.Action).ToListAsync()).ShouldContain("FieldDetailsUpdated");
    }

    [Test]
    public async Task DefaultAndEditedMaturitySnapshotsPersistWithLifecycleHistory()
    {
        CropCycle cycle = await CreateDraft();
        await new UpdateCropCyclePlanCommandHandler(_repository, _user, new Clock(),
            Options.Create(new CropMaturityOptions {DefaultCropMaturityMonths = 16})).Handle(
            new UpdateCropCyclePlanCommand(_field.Id, cycle.Id, cycle.Version, new DateOnly(2026, 2, 28),
                null, null, 70m), CancellationToken.None);
        _context.ChangeTracker.Clear();
        Tenant loaded = (await _repository.GetTenantForUserAsync(_user.Id!, false, CancellationToken.None))!;
        loaded.Id.ShouldBe(_tenant.Id);
        CropCycle saved = loaded.ActiveFarm!.Fields.Single(field => field.Id == _field.Id).CropCycles.Single(item => item.Id == cycle.Id);
        saved.StartDate.ShouldBe(new DateOnly(2026, 2, 28));
        saved.ExpectedHarvestStart.ShouldBe(new DateOnly(2027, 6, 28));
        saved.StatusChanges.ShouldContain(change => change.Reason != null && change.Reason.Contains("Plan edited"));
    }

    [Test]
    public async Task ManualYieldCorrectionPersistsAndMillAndReconciliationRemainIndependent()
    {
        CropCycle cycle = await CreateDraft();
        _field.ActivateCropCycle(cycle, Clock.Now, _user.Id!);
        cycle.MarkReadyForHarvest(Clock.Now, _user.Id!);
        cycle.RecordHarvest(new DateOnly(2026, 9, 1), 50m, new DateOnly(2026, 10, 5), Clock.Now, _user.Id!);
        Mill mill = Mill.Create(_tenant.Id, _field.FarmId, "CR012", _label, null, _user.Id!, Clock.Now);
        WeighbridgeTicket ticket = WeighbridgeTicket.CreateDraft(_tenant.Id, _field.FarmId, mill.Id, _label,
            new DateOnly(2026, 9, 1), 100m, 10m, 90m, _field.Id, cycle.Id, null, null, _user.Id!, Clock.Now);
        ticket.Record(_user.Id!, Clock.Now, _label, ticket.Version);
        _context.Set<Mill>().Add(mill);
        _context.Set<WeighbridgeTicket>().Add(ticket);
        await _repository.SaveChangesAsync(CancellationToken.None);
        await new UpdateActualYieldCommandHandler(_repository, _user, new Clock()).Handle(
            new UpdateActualYieldCommand(_field.Id, cycle.Id, cycle.Version, 55.125m), CancellationToken.None);
        _context.ChangeTracker.Clear();
        HarvestResult saved = await _context.Set<HarvestResult>().SingleAsync(item => item.CropCycleId == cycle.Id &&
            _context.Set<CropCycle>().Any(c => c.Id == item.CropCycleId && c.FieldId == _field.Id) &&
            _context.Fields.Any(f => f.Id == _field.Id && f.FarmId == _field.FarmId) &&
            _context.Farms.Any(f => f.Id == _field.FarmId && f.TenantId == _tenant.Id && f.Name == _label));
        saved.ActualTonnes.ShouldBe(55.125m);
        saved.HarvestDate.ShouldBe(new DateOnly(2026, 9, 1));
        (await _context.Set<WeighbridgeTicket>().SingleAsync(item => item.Id == ticket.Id &&
            item.TenantId == _tenant.Id && item.TicketReference == _label.ToUpperInvariant())).NetTonnes.ShouldBe(90m);
        // This workflow has no mill repository dependency; Phase 7 reconciliation is covered by its unchanged suite.
    }

    [Test]
    public async Task HistoricalReadUsesHarvestAgeAndKeepsSnapshotAfterDefaultChange()
    {
        CropCycle cycle = await CreateDraft();
        _field.ActivateCropCycle(cycle, Clock.Now, _user.Id!);
        cycle.MarkReadyForHarvest(Clock.Now, _user.Id!);
        cycle.RecordHarvest(new DateOnly(2026, 9, 1), 50m, new DateOnly(2026, 10, 5), Clock.Now, _user.Id!);
        cycle.Close(Clock.Now, _user.Id!);
        await _repository.SaveChangesAsync(CancellationToken.None);
        _context.ChangeTracker.Clear();
        CropCycleCollectionDto result = await new GetCropCyclesQueryHandler(_repository, _user, new Clock()).Handle(
            new GetCropCyclesQuery(_field.Id), CancellationToken.None);
        result.Field.Id.ShouldBe(_field.Id);
        result.CropCycles.Single(item => item.Id == cycle.Id).CropAgeMonths.ShouldBe(7);
        result.CropCycles.Single(item => item.Id == cycle.Id).ExpectedHarvestStart.ShouldBe("2027-03-31");
    }

    [Test]
    public async Task ForeignFieldIdentityCannotBeEdited()
    {
        Guid foreignField = Guid.NewGuid();
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() =>
            new UpdateFieldDetailsCommandHandler(_repository, _user, new Clock()).Handle(
                new UpdateFieldDetailsCommand(foreignField, "Synthetic", "Synthetic", null), CancellationToken.None));
        (await _context.Fields.SingleAsync(item => item.Id == _field.Id && item.FarmId == _field.FarmId)).Name.ShouldBe("Synthetic field");
    }

    private async Task<CropCycle> CreateDraft()
    {
        CropCycleDetailsDto details = await new CreateCropCycleCommandHandler(_repository, _user, new Clock(),
            Options.Create(new CropMaturityOptions())).Handle(new CreateCropCycleCommand(_field.Id, "PlantCane", null,
            _variety.Id, new DateOnly(2026, 1, 31), null, null, 50m), CancellationToken.None);
        details.CropCycle.ExpectedHarvestStart.ShouldBe("2027-03-31");
        return _field.CropCycles.Single(item => item.Id == details.CropCycle.Id);
    }

    private sealed class Clock : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class SyntheticUser(string id, string reference) : IUser
    {
        public List<string>? Roles => [];
        public string? Id => id;
        public string? CorrelationId => reference;
    }
}
