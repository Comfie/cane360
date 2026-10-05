using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Application.FarmSetup;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.CropCycles;

public sealed class FieldCropUxTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void DefaultMaturityIsFourteenMonthsAndMissingDateHasNoMaturity()
    {
        var options = new CropMaturityOptions();
        options.DefaultCropMaturityMonths.ShouldBe(14);
        options.Calculate(null).ShouldBeNull();
        options.Calculate(new DateOnly(2026, 10, 5)).ShouldBe(new DateOnly(2027, 12, 5));
    }

    [TestCase(2022, 12, 31, 2024, 2, 29)]
    [TestCase(2023, 12, 31, 2025, 2, 28)]
    [TestCase(2024, 2, 29, 2025, 4, 29)]
    public void MaturityUsesCalendarMonthEndAndLeapYearRules(int y, int m, int d, int ey, int em, int ed)
        => new CropMaturityOptions().Calculate(new DateOnly(y, m, d)).ShouldBe(new DateOnly(ey, em, ed));

    [Test]
    public void MaturityCanBeConfiguredWithoutChangingCalculationCode()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["CropCycles:DefaultCropMaturityMonths"] = "16" }).Build();
        var options = new CropMaturityOptions();
        configuration.GetSection(CropMaturityOptions.SectionName).Bind(options);
        options.Calculate(new DateOnly(2026, 10, 5)).ShouldBe(new DateOnly(2028, 2, 5));
    }

    [Test]
    public async Task CreatingCycleSnapshotsConfiguredMaturityAndReadsDoNotRecalculate()
    {
        var context = Context();
        var options = new CropMaturityOptions();
        var handler = new CreateCropCycleCommandHandler(context.Repository.Object, context.User.Object,
            new Clock(), Options.Create(options));
        var result = await handler.Handle(new CreateCropCycleCommand(context.Field.Id, "PlantCane", null,
            context.Variety.Id, new DateOnly(2026, 10, 5), null, null, 50m), CancellationToken.None);
        result.CropCycle.ExpectedHarvestStart.ShouldBe("2027-12-05");
        result.CropCycle.ExpectedHarvestEnd.ShouldBe("2027-12-05");
        result.CropCycle.CropAgeMonths.ShouldBe(0);
        options.DefaultCropMaturityMonths = 16;
        var read = await new GetCropCyclesQueryHandler(context.Repository.Object, context.User.Object, new Clock())
            .Handle(new GetCropCyclesQuery(context.Field.Id), CancellationToken.None);
        read.CropCycles.Single().ExpectedHarvestStart.ShouldBe("2027-12-05");
        var next = await handler.Handle(new CreateCropCycleCommand(context.Field.Id, "PlantCane", null,
            context.Variety.Id, new DateOnly(2026, 10, 5), null, null, 60m), CancellationToken.None);
        next.CropCycle.ExpectedHarvestStart.ShouldBe("2028-02-05");
    }

    [Test]
    public async Task DraftPlantingEditRecalculatesMaturityAndRecordsBeforeAndAfter()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        var handler = new UpdateCropCyclePlanCommandHandler(context.Repository.Object, context.User.Object,
            new Clock(), Options.Create(new CropMaturityOptions {DefaultCropMaturityMonths = 16}));
        var result = await handler.Handle(new UpdateCropCyclePlanCommand(context.Field.Id, cycle.Id, cycle.Version,
            new DateOnly(2026, 9, 30), null, null, 70m), CancellationToken.None);
        result.CropCycle.StartDate.ShouldBe("2026-09-30");
        result.CropCycle.ExpectedHarvestStart.ShouldBe("2028-01-30");
        result.CropCycle.Version.ShouldBe(1);
        result.Timeline.ShouldContain(item => item.Title == "Crop cycle edited" && item.Reason!.Contains("2026-01-31"));
    }

    [Test]
    public async Task ExplicitWindowIsPreservedAsMaturityOverride()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        await new UpdateCropCyclePlanCommandHandler(context.Repository.Object, context.User.Object,
                new Clock(), Options.Create(new CropMaturityOptions()))
            .Handle(new UpdateCropCyclePlanCommand(context.Field.Id, cycle.Id, cycle.Version,
                new DateOnly(2026, 2, 1), new DateOnly(2027, 6, 1), new DateOnly(2027, 7, 1), 70m), CancellationToken.None);
        cycle.ExpectedHarvestStart.ShouldBe(new DateOnly(2027, 6, 1));
        cycle.ExpectedHarvestEnd.ShouldBe(new DateOnly(2027, 7, 1));
    }

    [Test]
    public void ActiveAndHistoricalPlansCannotBeRecalculated()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.ActivateCropCycle(cycle, Now, "user-1");
        Should.Throw<InvalidOperationException>(() => cycle.UpdateDraftPlan(new DateOnly(2026, 2, 1),
            new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 1), 60m, Now, "user-1"));
        Harvest(context, cycle);
        cycle.Close(Now, "user-1");
        Should.Throw<InvalidOperationException>(() => cycle.UpdateDraftPlan(new DateOnly(2026, 2, 1),
            new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 1), 60m, Now, "user-1"));
        cycle.StartDate.ShouldBe(new DateOnly(2026, 1, 31));
        cycle.ExpectedHarvestStart.ShouldBe(new DateOnly(2027, 3, 31));
    }

    [Test]
    public async Task ActiveAgeUsesInjectedClockAndFutureStartIsZero()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.ActivateCropCycle(cycle, Now, "user-1");
        var result = await new GetCropCyclesQueryHandler(context.Repository.Object, context.User.Object, new Clock())
            .Handle(new GetCropCyclesQuery(context.Field.Id), CancellationToken.None);
        result.CropCycles.Single().CropAgeMonths.ShouldBe(8);
        cycle.AgeInMonths(new DateOnly(2025, 1, 1)).ShouldBe(0);
        cycle.AgeInMonths(new DateOnly(2026, 2, 28)).ShouldBe(1);
    }

    [Test]
    public void HarvestedAndClosedAgeStopsAtActualHarvestDate()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.ActivateCropCycle(cycle, Now, "user-1");
        Harvest(context, cycle);
        cycle.AgeInMonths(new DateOnly(2030, 1, 1)).ShouldBe(7);
        cycle.Close(Now, "user-1");
        cycle.AgeInMonths(new DateOnly(2040, 1, 1)).ShouldBe(7);
    }

    [Test]
    public async Task ActualYieldIsManuallyUpdatedWithoutChangingHarvestDateOrMillTicket()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.ActivateCropCycle(cycle, Now, "user-1");
        Harvest(context, cycle);
        WeighbridgeTicket ticket = WeighbridgeTicket.CreateDraft(context.Tenant.Id, context.Field.FarmId,
            Guid.NewGuid(), "CR012-MANUAL", new DateOnly(2026, 9, 1), 100m, 10m, 90m,
            context.Field.Id, cycle.Id, null, null, "user-1", Now);
        ticket.Record("user-1", Now, "CR012-TICKET", ticket.Version);
        long ticketVersion = ticket.Version;
        StatementTicketMatch match = StatementTicketMatch.Add(context.Tenant.Id, context.Field.FarmId,
            Guid.NewGuid(), ticket.Id, 90m, null, true, null, "user-1", Now, "CR012-MATCH");
        var result = await new UpdateActualYieldCommandHandler(context.Repository.Object, context.User.Object, new Clock())
            .Handle(new UpdateActualYieldCommand(context.Field.Id, cycle.Id, cycle.Version, 55.125m), CancellationToken.None);
        result.CropCycle.HarvestResult!.ActualTonnes.ShouldBe(55.125m);
        result.CropCycle.HarvestResult.HarvestDate.ShouldBe("2026-09-01");
        result.Timeline.ShouldContain(item => item.Reason != null && item.Reason.Contains("50.000 to 55.125"));
        ticket.NetTonnes.ShouldBe(90m);
        ticket.Version.ShouldBe(ticketVersion);
        match.MatchedTonnes.ShouldBe(90m);
        match.WeighbridgeTicketId.ShouldBe(ticket.Id);
        ticket.CropCycleId.ShouldBe(cycle.Id);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1000001)]
    [TestCase(1.1234)]
    public void InvalidManualYieldIsRejected(decimal tonnes)
        => new UpdateActualYieldCommandValidator().Validate(new UpdateActualYieldCommand(
            Guid.NewGuid(), Guid.NewGuid(), 0, tonnes)).IsValid.ShouldBeFalse();

    [Test]
    public void ClosedActualYieldCannotBeEdited()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.ActivateCropCycle(cycle, Now, "user-1");
        Harvest(context, cycle);
        cycle.Close(Now, "user-1");
        Should.Throw<InvalidOperationException>(() => cycle.UpdateActualYield(60m, Now, "user-1"));
        cycle.HarvestResult!.ActualTonnes.ShouldBe(50m);
    }

    [Test]
    public async Task YieldEditRejectsStaleVersionAndCrossTenantFieldWithoutSaving()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        var handler = new UpdateActualYieldCommandHandler(context.Repository.Object, context.User.Object, new Clock());
        await Should.ThrowAsync<ConflictException>(() => handler.Handle(
            new UpdateActualYieldCommand(context.Field.Id, cycle.Id, 99, 60m), CancellationToken.None));
        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(
            new UpdateActualYieldCommand(Guid.NewGuid(), cycle.Id, 0, 60m), CancellationToken.None));
        context.Repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SupervisorCannotUseYieldOrPlanOrPhysicalFieldWrites()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("supervisor");
        await Should.ThrowAsync<NotFoundException>(() => new UpdateActualYieldCommandHandler(repository.Object,
            user.Object, new Clock()).Handle(new UpdateActualYieldCommand(Guid.NewGuid(), Guid.NewGuid(), 0, 60m), CancellationToken.None));
        await Should.ThrowAsync<NotFoundException>(() => new UpdateCropCyclePlanCommandHandler(repository.Object,
            user.Object, new Clock(), Options.Create(new CropMaturityOptions())).Handle(new UpdateCropCyclePlanCommand(
                Guid.NewGuid(), Guid.NewGuid(), 0, new DateOnly(2026, 1, 1), null, null, 60m), CancellationToken.None));
        await Should.ThrowAsync<NotFoundException>(() => new UpdateFieldDetailsCommandHandler(repository.Object,
            user.Object, new Clock()).Handle(new UpdateFieldDetailsCommand(Guid.NewGuid(), "Field", "Furrow", null), CancellationToken.None));
    }

    [Test]
    public async Task FieldCreationRequiresOnlyPhysicalPropertiesAndEmptyCycleReadIsHonest()
    {
        var context = Context();
        var command = new CreateFieldCommand("B-01", "South", 5m, null, "Declared", "Furrow", null);
        new CreateFieldCommandValidator().Validate(command).IsValid.ShouldBeTrue();
        var result = await new CreateFieldCommandHandler(context.Repository.Object, context.User.Object)
            .Handle(command, CancellationToken.None);
        result.Farm!.Fields.Single(field => field.Code == "B-01").CurrentCropCycle.ShouldBeNull();
        var read = await new GetCropCyclesQueryHandler(context.Repository.Object, context.User.Object, new Clock())
            .Handle(new GetCropCyclesQuery(context.Field.Id), CancellationToken.None);
        read.CropCycles.ShouldBeEmpty();
    }

    [Test]
    public void PhysicalFieldEditLeavesCropHistoryAndAreasUnchanged()
    {
        var context = Context();
        CropCycle cycle = Draft(context);
        context.Field.UpdateDetails("Renamed", "Drip", "Clay");
        context.Field.CropCycles.Single().ShouldBeSameAs(cycle);
        context.Field.DeclaredHectares.ShouldBe(5m);
        cycle.StartDate.ShouldBe(new DateOnly(2026, 1, 31));
        context.Field.Name.ShouldBe("Renamed");
    }

    [Test]
    public void FieldValidationAndPartialHarvestWindowValidationRemainRequired()
    {
        new CreateFieldCommandValidator().Validate(new CreateFieldCommand("BAD CODE", "", -1m, null,
            "Mapped", "", null)).IsValid.ShouldBeFalse();
        new CreateCropCycleCommandValidator().Validate(new CreateCropCycleCommand(Guid.NewGuid(), "PlantCane", null,
            Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, 50m)).IsValid.ShouldBeFalse();
        new CreateCropCycleCommandValidator().Validate(new CreateCropCycleCommand(Guid.NewGuid(), "PlantCane", null,
            Guid.NewGuid(), default, null, null, 50m)).IsValid.ShouldBeFalse();
    }

    private static CropCycle Draft(Fixture context) => context.Field.CreateCropCycleDraft(CropCycleType.PlantCane,
        null, context.Variety, context.Variety.Name, new DateOnly(2026, 1, 31), new DateOnly(2027, 3, 31),
        new DateOnly(2027, 3, 31), 50m, Now, "user-1");

    private static void Harvest(Fixture context, CropCycle cycle)
    {
        cycle.MarkReadyForHarvest(Now, "user-1");
        cycle.RecordHarvest(new DateOnly(2026, 9, 1), 50m, new DateOnly(2026, 10, 5), Now, "user-1");
    }

    private static Fixture Context()
    {
        var tenant = Tenant.CreateForGrower("user-1", "CR012 Synthetic", null);
        var farm = tenant.CreateFarm("CR012", "Synthetic Farm", "Test", "Test", "Lease", 10m, "Furrow");
        Field field = farm.AddField("A-01", "North", 5m, null, ReportingAreaSource.Declared, "Furrow", null);
        CropVariety variety = tenant.AddCropVariety("N14", "N14");
        var repository = new Mock<IFarmSetupRepository>();
        repository.Setup(value => value.GetTenantForUserAsync("user-1", It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        repository.Setup(value => value.GetTenantForOperationalUserAsync("user-1", false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("user-1");
        return new Fixture(tenant, field, variety, repository, user);
    }

    private sealed record Fixture(Tenant Tenant, Field Field, CropVariety Variety,
        Mock<IFarmSetupRepository> Repository, Mock<IUser> User);
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
