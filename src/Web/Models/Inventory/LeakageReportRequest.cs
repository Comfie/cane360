namespace Cane360.Web.Models.Inventory;

public sealed record LeakageReportRequest(string? FromDate, string? ToDate, Guid? FieldId, Guid? CropCycleId,
    Guid? ActivityId, Guid? InventoryItemId, Guid? InventoryLotId, Guid? IssuerPersonId, Guid? RecipientPersonId,
    Guid? SupervisorPersonId, string? Status, string? ExceptionType, string? Severity, int Page = 1, int PageSize = 50);
