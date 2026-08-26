namespace Cane360.Application.Inventory;

public sealed record LeakageReportFilter(DateOnly? FromDate, DateOnly? ToDate, Guid? FieldId, Guid? CropCycleId,
    Guid? ActivityId, Guid? InventoryItemId, Guid? InventoryLotId, Guid? IssuerPersonId, Guid? RecipientPersonId,
    Guid? SupervisorPersonId, string? Status, string? ExceptionType, string? Severity, int Page, int PageSize);
