using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Inventory;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public class InventoryPostingTests
{
    [Test]
    public async Task ReceiptPostingLocksStoreSourceAndPositionBeforeWritingMovements()
    {
        var tenant = Tenant.CreateForGrower("grower-1", "Grower", null);
        var farm = tenant.CreateFarm("AUTOTEST", "Test Farm", "Address", "Location", "Lease", 10m, "Furrow");
        var unit = UnitOfMeasure.Create(tenant.Id, "KG", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(
            tenant.Id, farm.Id, "ITEM-1", "Compound D", InventoryItemCategory.Fertiliser,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var receipt = StockReceipt.Create(
            tenant.Id, farm.Id, farm.Store.Id, StockReceiptType.Purchase, Guid.NewGuid(),
            new DateOnly(2026, 8, 22), null, "GRN-1", null, null, 0);
        receipt.AddLine(item, null, 20m, 2.5m, receipt.Version);
        var position = StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null);
        var calls = new List<string>();
        var transaction = new Mock<IInventoryTransaction>();
        transaction.Setup(value => value.CommitAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("commit"))
            .Returns(Task.CompletedTask);
        var repository = new Mock<IInventoryRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        repository.Setup(value => value.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("store"))
            .Returns(Task.CompletedTask);
        repository.Setup(value => value.LockReceiptSourceAsync(tenant.Id, farm.Id, receipt.Id, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("source"))
            .Returns(Task.CompletedTask);
        repository.Setup(value => value.GetReceiptAsync(
                tenant.Id, farm.Id, receipt.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        repository.Setup(value => value.GetPositionAsync(
                tenant.Id, farm.Id, farm.Store.Id, item.Id, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);
        repository.Setup(value => value.LockStockPositionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("position"))
            .Returns(Task.CompletedTask);
        StockMovement? movement = null;
        repository.Setup(value => value.Add(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(value => movement = value);
        repository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("save"))
            .ReturnsAsync(1);
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(value => value.GetTenantForUserAsync(
                "grower-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-1");
        user.Setup(value => value.CorrelationId).Returns("p5a-test");
        var handler = new PostStockReceiptCommandHandler(
            farmRepository.Object, repository.Object, user.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero)));

        var result = await handler.Handle(
            new PostStockReceiptCommand(receipt.Id, receipt.Version, "post-key-1"), CancellationToken.None);

        calls.ShouldBe(["store", "source", "position", "save", "commit"]);
        movement.ShouldNotBeNull();
        movement.SignedQuantity.ShouldBe(20m);
        movement.SignedValueUsd.ShouldBe(50m);
        result.Status.ShouldBe(nameof(StockReceiptStatus.Posted));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
