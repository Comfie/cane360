using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record CreateFieldAccountabilityCorrectionCommand(
    Guid? FieldReceiptId,
    Guid? InputApplicationId,
    Guid? StockReturnId,
    Guid? InventoryLossId,
    long SourceVersion,
    string Reason,
    string IdempotencyKey) : IRequest<Guid>;
