namespace Cane360.Domain.Inventory;

public sealed class ControlException : BaseEntity
{
    private ControlException() { }
    private ControlException(Guid tenantId, Guid farmId, Guid activityId, Guid issueLineId, string code, decimal issued, decimal applied, decimal returned, decimal loss, decimal unaccounted, DateTimeOffset openedAt)
    { TenantId = tenantId; FarmId = farmId; ActivityId = activityId; StockIssueLineId = issueLineId; Code = code; IssuedQuantity = issued; AppliedQuantity = applied; ReturnedQuantity = returned; ApprovedLossQuantity = loss; UnaccountedQuantity = unaccounted; OpenedAt = openedAt; Status = ControlExceptionStatus.Open; }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid ActivityId { get; private set; } public Guid StockIssueLineId { get; private set; } public string Code { get; private set; } = string.Empty;
    public decimal IssuedQuantity { get; private set; } public decimal AppliedQuantity { get; private set; } public decimal ReturnedQuantity { get; private set; } public decimal ApprovedLossQuantity { get; private set; } public decimal UnaccountedQuantity { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; } public DateTimeOffset? ResolvedAt { get; private set; } public ControlExceptionStatus Status { get; private set; }
    public static ControlException Open(Guid tenantId, Guid farmId, Guid activityId, Guid issueLineId, decimal issued, decimal applied, decimal returned, decimal loss, decimal unaccounted, DateTimeOffset openedAt) => new(tenantId, farmId, activityId, issueLineId, "InventoryUnaccounted", issued, applied, returned, loss, unaccounted, openedAt);
    public void Resolve(decimal applied, decimal returned, decimal loss, DateTimeOffset resolvedAt)
    { if (Status != ControlExceptionStatus.Open) return; AppliedQuantity = applied; ReturnedQuantity = returned; ApprovedLossQuantity = loss; UnaccountedQuantity = 0; ResolvedAt = resolvedAt; Status = ControlExceptionStatus.Resolved; }
}
