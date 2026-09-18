using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateStockAdjustmentCommand(Guid? StockCountLineId, Guid? InventoryItemId, Guid? InventoryLotId,
    string AdjustmentType, decimal? SignedQuantity, decimal? ExplicitUnitValueUsd, string Reason, DateOnly EventDate) : IRequest<StockAdjustmentDto>;
