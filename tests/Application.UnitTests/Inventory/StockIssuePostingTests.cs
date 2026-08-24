using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class StockIssuePostingTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task IssuePostingLocksInRequiredOrderAndSnapshotsMovingAverage()
    {
        var setup = CreateSetup();
        var calls = new List<string>();
        var transaction = new Mock<IInventoryTransaction>();
        transaction.Setup(value => value.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("commit")).Returns(Task.CompletedTask);
        var repository = new Mock<IInventoryRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository.Setup(value => value.LockStoreAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Farm.Store.Id, It.IsAny<CancellationToken>())).Callback(() => calls.Add("store"))
            .Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockIssueAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Issue.Id, It.IsAny<CancellationToken>())).Callback(() => calls.Add("source"))
            .Returns(Task.CompletedTask);
        repository.Setup(value => value.LockInputRequestLinesAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("request-lines")).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockPositionsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("positions")).Returns(Task.CompletedTask);
        repository.Setup(value => value.GetStockIssueAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Issue.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(setup.Issue);
        repository.Setup(value => value.GetInputRequestAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Request.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Request);
        repository.Setup(value => value.GetPostedIssueQuantityAsync(
            setup.RequestLine.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        repository.Setup(value => value.GetPositionSnapshotAsync(
            setup.Position.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new StockLedgerSnapshot(100m, 300m));
        repository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save")).ReturnsAsync(1);
        StockMovement? movement = null;
        repository.Setup(value => value.Add(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(value => movement = value);
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
            "grower-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-user");
        user.Setup(value => value.CorrelationId).Returns("p5b-issue-test");
        var handler = new PostStockIssueCommandHandler(farmRepository.Object, repository.Object,
            user.Object, new FixedTimeProvider(Now));

        await handler.Handle(new PostStockIssueCommand(
            setup.Issue.Id, setup.Issue.Version, "issue-key"), CancellationToken.None);

        calls.ShouldBe(["store", "source", "request-lines", "positions", "save", "commit"]);
        setup.Issue.Status.ShouldBe(StockIssueStatus.Posted);
        setup.Issue.Lines.Single().IssueUnitCostUsd.ShouldBe(3m);
        movement.ShouldNotBeNull();
        movement.SignedQuantity.ShouldBe(-40m);
        movement.SignedValueUsd.ShouldBe(-120m);
        setup.Request.Status.ShouldBe(InputRequestStatus.PartiallyIssued);
    }

    [Test]
    public async Task IssuePostingRejectsQuantityAboveStockWithoutMovement()
    {
        var setup = CreateSetup();
        var repository = RepositoryForConflict(setup, new StockLedgerSnapshot(20m, 60m));
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
            "grower-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-user");
        var handler = new PostStockIssueCommandHandler(farmRepository.Object, repository.Object,
            user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ConflictException>(() => handler.Handle(
            new PostStockIssueCommand(setup.Issue.Id, setup.Issue.Version, "insufficient"), CancellationToken.None));
        repository.Verify(value => value.Add(It.IsAny<StockMovement>()), Times.Never);
    }

    [Test]
    public async Task IssuePostingRejectsQuantityAboveApprovedOutstandingWithoutMovement()
    {
        var setup = CreateSetup();
        var repository = RepositoryForConflict(setup, new StockLedgerSnapshot(100m, 300m));
        repository.Setup(value => value.GetPostedIssueQuantityAsync(
            setup.RequestLine.Id, It.IsAny<CancellationToken>())).ReturnsAsync(70m);
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
            "grower-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-user");
        var handler = new PostStockIssueCommandHandler(farmRepository.Object, repository.Object,
            user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ConflictException>(() => handler.Handle(
            new PostStockIssueCommand(setup.Issue.Id, setup.Issue.Version, "over-approved"), CancellationToken.None));
        repository.Verify(value => value.Add(It.IsAny<StockMovement>()), Times.Never);
    }

    [Test]
    public async Task FarmManagerCannotAuthoriseIssueReversal()
    {
        var setup = CreateSetup();
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
            "manager-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("manager-user");
        var handler = new ReverseStockIssueCommandHandler(farmRepository.Object,
            new Mock<IInventoryRepository>().Object, user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ForbiddenAccessException>(() => handler.Handle(
            new ReverseStockIssueCommand(setup.Issue.Id, setup.Issue.Version,
                "Not authorised", "manager-reversal"), CancellationToken.None));
    }

    [Test]
    public async Task DependentFieldAccountabilityBlocksIssueReversal()
    {
        var setup = CreateSetup();
        setup.Issue.MarkPosted(Now, "grower-user", "posted-before-dependent", setup.Issue.Version);
        var transaction = new Mock<IInventoryTransaction>();
        var repository = new Mock<IInventoryRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository.Setup(value => value.GetStockIssueAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Issue.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(setup.Issue);
        repository.Setup(value => value.LockStoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockIssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockInputRequestLinesAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockPositionsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.HasDependentFieldAccountabilityAsync(
            setup.Issue.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
            "grower-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-user");
        var handler = new ReverseStockIssueCommandHandler(farmRepository.Object,
            repository.Object, user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ConflictException>(() => handler.Handle(
            new ReverseStockIssueCommand(setup.Issue.Id, setup.Issue.Version,
                "Dependent records", "dependent-reversal"), CancellationToken.None));
        repository.Verify(value => value.Add(It.IsAny<StockMovement>()), Times.Never);
    }

    private static Mock<IInventoryRepository> RepositoryForConflict(
        PostingSetup setup, StockLedgerSnapshot snapshot)
    {
        var transaction = new Mock<IInventoryTransaction>();
        var repository = new Mock<IInventoryRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository.Setup(value => value.LockStoreAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockIssueAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockInputRequestLinesAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.LockStockPositionsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(value => value.GetStockIssueAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Issue.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(setup.Issue);
        repository.Setup(value => value.GetInputRequestAsync(setup.Tenant.Id, setup.Farm.Id,
            setup.Request.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Request);
        repository.Setup(value => value.GetPostedIssueQuantityAsync(
            setup.RequestLine.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        repository.Setup(value => value.GetPositionSnapshotAsync(
            setup.Position.Id, It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
        return repository;
    }

    private static PostingSetup CreateSetup()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Grower", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var type = tenant.AddActivityType("FERT", "Fertilising", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Lease", 20m, "Furrow");
        var manager = farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1));
        tenant.AddFarmManagerMembership("manager-user", manager.Id);
        var supervisor = farm.AddPerson("Supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var storekeeper = farm.AddPerson("Storekeeper", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(storekeeper, PersonRole.Storekeeper, false, new DateOnly(2026, 1, 1));
        var recipient = farm.AddPerson("Recipient", null, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A1", "Block A", 10m, null,
            ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31),
            800m, Now, "grower-user");
        field.ActivateCropCycle(cycle, Now, "grower-user");
        var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 22), supervisor.Id);
        var unit = UnitOfMeasure.Create(tenant.Id, "KG", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(tenant.Id, farm.Id, "FERT-1", "Fertiliser",
            InventoryItemCategory.Fertiliser, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var rule = InventoryApplicationRule.Create(tenant.Id, farm.Id, item, type.Id,
            new DateOnly(2026, 1, 1), null, ApplicationCoverageBasis.FieldReportingHectares,
            10m, 0m, 0m);
        var request = InputRequest.Create(tenant.Id, farm.Id, field.Id, cycle.Id, activity.Id,
            new DateOnly(2026, 8, 22), "grower-user");
        var requestLine = request.AddLine(item, rule, 10m, 100m, 100m, 3m, request.Version);
        request.Submit(Now, "submit", request.Version);
        request.OpenApproval(request.Version);
        request.Decide(ApprovalOutcome.Approved, null, Now, request.Version);
        var position = StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null);
        var issue = StockIssue.Create(tenant.Id, farm.Id, farm.Store.Id, request.Id,
            new DateOnly(2026, 8, 22), storekeeper.Id, recipient.Id, null, 0);
        issue.AddLine(requestLine, position.Id, null, null, 40m, issue.Version);
        return new(tenant, farm, request, requestLine, position, issue);
    }

    private sealed record PostingSetup(Tenant Tenant, Farm Farm, InputRequest Request,
        InputRequestLine RequestLine, StockPosition Position, StockIssue Issue);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
