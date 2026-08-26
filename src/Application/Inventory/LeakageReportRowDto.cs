namespace Cane360.Application.Inventory;

public sealed record LeakageReportRowDto(string ExceptionType, string Severity, string Status, DateOnly EventDate,
    Guid? FieldId, Guid? CropCycleId, Guid? ActivityId, Guid? InventoryItemId, Guid? InventoryLotId,
    Guid? IssuerPersonId, Guid? RecipientPersonId, Guid? SupervisorPersonId, decimal Quantity,
    decimal ValueUsd, string UnitCode, IReadOnlyList<Guid> SourceChainIds, string TraceSummary);
