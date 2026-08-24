namespace Cane360.Application.Inventory;

public sealed record InputAccountabilityDto(Guid StockIssueId, Guid StockIssueLineId, Guid ActivityId,
    string ItemCode, string? LotCode, string UnitCode, decimal IssuedQuantity,
    decimal FieldReceivedQuantity, decimal ConfirmedAppliedQuantity, decimal PostedReturnedQuantity,
    decimal ApprovedLossQuantity, decimal UnaccountedQuantity, bool IsBlocking);
