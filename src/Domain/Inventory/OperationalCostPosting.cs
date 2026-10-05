using Cane360.Domain.Finance;
using Cane360.Domain.Payroll;

namespace Cane360.Domain.Inventory;

public sealed class OperationalCostPosting : BaseEntity
{
    private OperationalCostPosting() { }

    private OperationalCostPosting(Guid tenantId, Guid farmId, Guid fieldId, Guid? activityId, Guid cropCycleId,
        OperationalCostCategory category,
        Guid? applicationLineId, Guid? inventoryLossId, Guid? payrollEarningLineId, Guid? transactionAllocationId,
        decimal sourceQuantity, decimal unitCostUsd, string postingIdentity, Guid? reversalOfId)
    {
        if (sourceQuantity <= 0)
        {
            throw new InvalidOperationException("Cost source quantity must be positive.");
        }

        TenantId = tenantId;
        FarmId = farmId;
        FieldId = fieldId;
        ActivityId = activityId;
        CropCycleId = cropCycleId;
        Category = category;
        InputApplicationLineId = applicationLineId;
        InventoryLossId = inventoryLossId;
        PayrollEarningLineId = payrollEarningLineId;
        TransactionAllocationId = transactionAllocationId;
        SourceQuantitySnapshot = decimal.Round(sourceQuantity, 6, MidpointRounding.AwayFromZero);
        UnitCostUsdSnapshot = decimal.Round(unitCostUsd, 6, MidpointRounding.AwayFromZero);
        AmountUsd = decimal.Round(sourceQuantity * unitCostUsd, 2, MidpointRounding.AwayFromZero);
        PostingIdentity = postingIdentity.Trim();
        ReversalOfOperationalCostPostingId = reversalOfId;
    }

    public Guid TenantId { get; }
    public Guid FarmId { get; }
    public Guid FieldId { get; }
    public Guid? ActivityId { get; }
    public Guid CropCycleId { get; }
    public OperationalCostCategory Category { get; }
    public Guid? InputApplicationLineId { get; }
    public Guid? InventoryLossId { get; }
    public Guid? PayrollEarningLineId { get; }
    public Guid? TransactionAllocationId { get; }
    public decimal SourceQuantitySnapshot { get; }
    public decimal UnitCostUsdSnapshot { get; }
    public decimal AmountUsd { get; private set; }
    public string PostingIdentity { get; private set; } = string.Empty;
    public Guid? ReversalOfOperationalCostPostingId { get; private set; }

    public static OperationalCostPosting ForApplication(Guid tenantId, Guid farmId, Guid fieldId, Guid activityId,
        Guid cycleId, InputApplicationLine line, string identity)
    {
        return new OperationalCostPosting(tenantId, farmId, fieldId, activityId, cycleId,
            OperationalCostCategory.AppliedInput, line.Id, null,
            null, null, line.AppliedQuantity, line.IssueUnitCostUsdSnapshot, identity, null);
    }

    public static OperationalCostPosting ForLoss(Guid tenantId, Guid farmId, Guid fieldId, Guid activityId,
        Guid cycleId, InventoryLoss loss, string identity)
    {
        return new OperationalCostPosting(tenantId, farmId, fieldId, activityId, cycleId,
            OperationalCostCategory.ApprovedInventoryLoss, null,
            loss.Id, null, null, loss.Quantity, loss.IssueUnitCostUsdSnapshot, identity, null);
    }

    public static OperationalCostPosting ForPayroll(Guid tenantId, Guid farmId, Guid fieldId, Guid? activityId,
        Guid cycleId, PayrollEarningLine line, string identity)
    {
        return new OperationalCostPosting(tenantId, farmId, fieldId, activityId, cycleId,
            OperationalCostCategory.Labour, null, null, line.Id,
            null, line.Quantity, line.RateAmountUsd, identity, null);
    }

    public static OperationalCostPosting ForDirectExpense(Guid tenantId, Guid farmId, Guid fieldId, Guid cycleId,
        TransactionAllocation allocation, string identity)
    {
        return new OperationalCostPosting(tenantId, farmId, fieldId, null, cycleId,
            OperationalCostCategory.DirectExpense, null, null, null,
            allocation.Id, 1m, allocation.AmountUsd, identity, null);
    }

    public static OperationalCostPosting Reverse(OperationalCostPosting original, string identity)
    {
        return new OperationalCostPosting(original.TenantId, original.FarmId, original.FieldId, original.ActivityId,
            original.CropCycleId,
            original.Category, original.InputApplicationLineId, original.InventoryLossId, original.PayrollEarningLineId,
            original.TransactionAllocationId, original.SourceQuantitySnapshot, -original.UnitCostUsdSnapshot, identity,
            original.Id);
    }
}
