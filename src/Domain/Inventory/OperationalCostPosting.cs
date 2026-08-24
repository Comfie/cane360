namespace Cane360.Domain.Inventory;

public sealed class OperationalCostPosting : BaseEntity
{
    private OperationalCostPosting() { }
    private OperationalCostPosting(Guid tenantId, Guid farmId, Guid fieldId, Guid activityId, Guid cropCycleId, OperationalCostCategory category,
        Guid? applicationLineId, Guid? inventoryLossId, decimal sourceQuantity, decimal unitCostUsd, string postingIdentity, Guid? reversalOfId)
    {
        if (sourceQuantity <= 0) throw new InvalidOperationException("Cost source quantity must be positive.");
        TenantId = tenantId; FarmId = farmId; FieldId = fieldId; ActivityId = activityId; CropCycleId = cropCycleId; Category = category; InputApplicationLineId = applicationLineId; InventoryLossId = inventoryLossId;
        SourceQuantitySnapshot = decimal.Round(sourceQuantity, 6, MidpointRounding.AwayFromZero); UnitCostUsdSnapshot = decimal.Round(unitCostUsd, 6, MidpointRounding.AwayFromZero);
        AmountUsd = decimal.Round(sourceQuantity * unitCostUsd, 2, MidpointRounding.AwayFromZero); PostingIdentity = postingIdentity.Trim(); ReversalOfOperationalCostPostingId = reversalOfId;
    }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid FieldId { get; private set; } public Guid ActivityId { get; private set; } public Guid CropCycleId { get; private set; }
    public OperationalCostCategory Category { get; private set; } public Guid? InputApplicationLineId { get; private set; } public Guid? InventoryLossId { get; private set; }
    public decimal SourceQuantitySnapshot { get; private set; } public decimal UnitCostUsdSnapshot { get; private set; } public decimal AmountUsd { get; private set; }
    public string PostingIdentity { get; private set; } = string.Empty; public Guid? ReversalOfOperationalCostPostingId { get; private set; }
    public static OperationalCostPosting ForApplication(Guid tenantId, Guid farmId, Guid fieldId, Guid activityId, Guid cycleId, InputApplicationLine line, string identity) =>
        new(tenantId, farmId, fieldId, activityId, cycleId, OperationalCostCategory.AppliedInput, line.Id, null, line.AppliedQuantity, line.IssueUnitCostUsdSnapshot, identity, null);
    public static OperationalCostPosting ForLoss(Guid tenantId, Guid farmId, Guid fieldId, Guid activityId, Guid cycleId, InventoryLoss loss, string identity) =>
        new(tenantId, farmId, fieldId, activityId, cycleId, OperationalCostCategory.ApprovedInventoryLoss, null, loss.Id, loss.Quantity, loss.IssueUnitCostUsdSnapshot, identity, null);
    public static OperationalCostPosting Reverse(OperationalCostPosting original, string identity) =>
        new(original.TenantId, original.FarmId, original.FieldId, original.ActivityId, original.CropCycleId, original.Category, original.InputApplicationLineId, original.InventoryLossId, original.SourceQuantitySnapshot, -original.UnitCostUsdSnapshot, identity, original.Id);
}
