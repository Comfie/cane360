using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateFieldReceiptCommand(Guid StockIssueId, Guid FieldId, Guid CropCycleId, Guid ActivityId,
    Guid RecipientPersonId, DateTimeOffset ReceivedAt, string? LateEntryReason,
    IReadOnlyList<CreateFieldReceiptLineCommand> Lines) : IRequest<Guid>;
