namespace Cane360.Domain.Inventory;

public sealed class InventoryLoss : BaseAuditableEntity
{
    private InventoryLoss() { }
    private InventoryLoss(Guid tenantId, Guid farmId, Guid activityId, StockIssueLine issueLine, decimal quantity, InventoryLossType lossType, string reason, string submittedByUserId)
    {
        if (quantity <= 0) throw new InvalidOperationException("Loss quantity must be positive."); ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        TenantId = tenantId; FarmId = farmId; ActivityId = activityId; StockIssueLineId = issueLine.Id; InventoryItemId = issueLine.InventoryItemId; InventoryLotId = issueLine.InventoryLotId;
        UnitOfMeasureId = issueLine.UnitOfMeasureId; ItemCodeSnapshot = issueLine.ItemCodeSnapshot; ItemNameSnapshot = issueLine.ItemNameSnapshot; LotCodeSnapshot = issueLine.LotCodeSnapshot; UnitCodeSnapshot = issueLine.UnitCodeSnapshot;
        IssueUnitCostUsdSnapshot = issueLine.IssueUnitCostUsd!.Value; Quantity = decimal.Round(quantity, 6, MidpointRounding.AwayFromZero); LossType = lossType; Reason = reason.Trim(); SubmittedByUserId = submittedByUserId.Trim(); Status = InventoryLossStatus.Draft; Version = 1;
    }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid ActivityId { get; private set; } public Guid StockIssueLineId { get; private set; } public Guid InventoryItemId { get; private set; } public Guid? InventoryLotId { get; private set; } public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty; public string ItemNameSnapshot { get; private set; } = string.Empty; public string? LotCodeSnapshot { get; private set; } public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal IssueUnitCostUsdSnapshot { get; private set; } public decimal Quantity { get; private set; } public InventoryLossType LossType { get; private set; } public string Reason { get; private set; } = string.Empty; public string SubmittedByUserId { get; private set; } = string.Empty;
    public InventoryLossStatus Status { get; private set; } public DateTimeOffset? SubmittedAt { get; private set; } public DateTimeOffset? DecidedAt { get; private set; } public long Version { get; private set; }
    public static InventoryLoss Create(Guid tenantId, Guid farmId, Guid activityId, StockIssueLine issueLine, decimal quantity, InventoryLossType lossType, string reason, string submittedByUserId) => new(tenantId, farmId, activityId, issueLine, quantity, lossType, reason, submittedByUserId);
    public void Submit(DateTimeOffset submittedAt, long expectedVersion) { Require(expectedVersion); if (Status != InventoryLossStatus.Draft) throw new InvalidOperationException("Only a draft loss can be submitted."); SubmittedAt = submittedAt; Status = InventoryLossStatus.Submitted; Version++; }
    public void Decide(ApprovalOutcome outcome, DateTimeOffset decidedAt, long expectedVersion) { Require(expectedVersion); if (Status != InventoryLossStatus.Submitted) throw new InvalidOperationException("Only a submitted loss can be decided."); Status = outcome == ApprovalOutcome.Approved ? InventoryLossStatus.Approved : InventoryLossStatus.Rejected; DecidedAt = decidedAt; Version++; }
    public void Supersede(long expectedVersion) { Require(expectedVersion); if (Status != InventoryLossStatus.Approved) throw new InvalidOperationException("Only an approved loss can be superseded."); Status = InventoryLossStatus.Superseded; Version++; }
    private void Require(long version) { if (Version != version) throw new InvalidOperationException("This loss changed after it was loaded. Refresh and try again."); }
}
