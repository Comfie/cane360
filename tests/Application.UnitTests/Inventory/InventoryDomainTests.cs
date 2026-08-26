using Cane360.Domain.Inventory;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public class InventoryDomainTests
{
    [Test]
    public void ReceiptLinesPreserveStockUnitAndValueSnapshots()
    {
        var tenantId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "kg", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(
            tenantId, farmId, "FERT-01", "Compound D", InventoryItemCategory.Fertiliser,
            unit, 20m, LotTrackingPolicy.Optional, ExpiryPolicy.Optional);
        var receipt = StockReceipt.Create(
            tenantId, farmId, Guid.NewGuid(), StockReceiptType.Purchase, Guid.NewGuid(),
            new DateOnly(2026, 8, 22), null, "GRN-1", null, null, 0);

        var line = receipt.AddLine(item, null, 12.3456789m, 2.1234567m, receipt.Version);

        line.UnitCodeSnapshot.ShouldBe("KG");
        line.Quantity.ShouldBe(12.345679m);
        line.UnitCostUsd.ShouldBe(2.123457m);
        line.LineValueUsd.ShouldBe(26.215518m);
    }

    [Test]
    public void OpeningBalanceRequiresReasonAndApprovalBeforePosting()
    {
        var tenantId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "l", "Litre", "Volume", 3);
        var item = InventoryItem.Create(
            tenantId, farmId, "CHEM-1", "Herbicide", InventoryItemCategory.Chemical,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var receipt = StockReceipt.Create(
            tenantId, farmId, Guid.NewGuid(), StockReceiptType.OpeningBalance, null,
            new DateOnly(2026, 8, 22), null, "OPEN-1", "Verified store count", null, 0);
        receipt.AddLine(item, null, 10m, 5m, receipt.Version);

        Should.Throw<InvalidOperationException>(() =>
            receipt.MarkPosted(DateTimeOffset.UtcNow, "grower", "post-1", receipt.Version));

        receipt.SubmitOpeningBalance(receipt.Version);
        var submittedVersion = receipt.Version;
        receipt.RecordOpeningDecision(ApprovalOutcome.Approved, submittedVersion);
        receipt.MarkPosted(DateTimeOffset.UtcNow, "grower", "post-1", receipt.Version);

        receipt.Status.ShouldBe(StockReceiptStatus.Posted);
    }

    [Test]
    public void ItemPolicyControlsLotsWithoutNameBasedFuelRules()
    {
        var tenantId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "l", "Litre", "Volume", 3);

        var genericItem = InventoryItem.Create(
            tenantId, farmId, "GEN-1", "Fuel additive", InventoryItemCategory.Other,
            unit, null, LotTrackingPolicy.Optional, ExpiryPolicy.Optional);
        var lot = InventoryLot.Create(tenantId, farmId, genericItem, "BATCH-1", null);

        lot.InventoryItemId.ShouldBe(genericItem.Id);
    }

    [Test]
    public void CountPreservesExpectedSnapshotAndAllowsEntryCorrectionOnlyWhileInProgress()
    {
        var tenantId = Guid.NewGuid(); var farmId = Guid.NewGuid(); var storeId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "kg", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(tenantId, farmId, "FERT-1", "Fertiliser", InventoryItemCategory.Fertiliser, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var position = StockPosition.Create(tenantId, farmId, storeId, item.Id, null);
        var count = StockCount.Create(tenantId, farmId, storeId, "Morning count", "A. Counter", new DateOnly(2026, 8, 25), "manager");
        var line = StockCountLine.Create(count, position, item, null, unit, 10m, 25m);
        count.Start(42, [line], DateTimeOffset.UtcNow, count.Version);

        line.Enter(8m, "First entry", DateTimeOffset.UtcNow, "manager", line.Version);
        line.Enter(9m, "Corrected entry", DateTimeOffset.UtcNow, "manager", line.Version);

        count.CutoffPostingSequence.ShouldBe(42); line.ExpectedQuantity.ShouldBe(10m); line.ExpectedValueUsd.ShouldBe(25m); line.VarianceQuantity.ShouldBe(-1m);
        count.MoveToReview(DateTimeOffset.UtcNow, count.Version);
        count.Status.ShouldBe(StockCountStatus.Review);
    }

    [Test]
    public void CountClosesOnlyWhenEveryVarianceHasPostedAdjustment()
    {
        var tenantId = Guid.NewGuid(); var farmId = Guid.NewGuid(); var storeId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "kg", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(tenantId, farmId, "FERT-1", "Fertiliser", InventoryItemCategory.Fertiliser, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var position = StockPosition.Create(tenantId, farmId, storeId, item.Id, null); var count = StockCount.Create(tenantId, farmId, storeId, "", "Counter", new DateOnly(2026, 8, 25), "manager"); var line = StockCountLine.Create(count, position, item, null, unit, 10m, 25m);
        count.Start(1, [line], DateTimeOffset.UtcNow, count.Version); line.Enter(8m, null, DateTimeOffset.UtcNow, "manager", line.Version); count.MoveToReview(DateTimeOffset.UtcNow, count.Version); count.ResolveReview(DateTimeOffset.UtcNow, count.Version);

        count.Status.ShouldBe(StockCountStatus.PendingAdjustment);
        Should.Throw<InvalidOperationException>(() => count.CloseAfterAdjustments(DateTimeOffset.UtcNow));
        line.Resolve(Guid.NewGuid()); count.CloseAfterAdjustments(DateTimeOffset.UtcNow);
        count.Status.ShouldBe(StockCountStatus.Closed);
    }
}
