using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateStockReturnCommand(Guid ActivityId, DateOnly ReturnDate, Guid SenderPersonId, Guid ReceiverPersonId,
    IReadOnlyList<CreateStockReturnLineCommand> Lines) : IRequest<Guid>;
