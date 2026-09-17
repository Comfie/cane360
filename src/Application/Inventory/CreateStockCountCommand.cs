using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateStockCountCommand(DateOnly EventDate, string Notes, string CountingPersons) : IRequest<StockCountDto>;
