using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class GetStockReceiptQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockReceiptQuery, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(GetStockReceiptQuery request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var receipt = await inventoryRepository.GetReceiptAsync(
            tenant.Id, farm.Id, request.ReceiptId, false, cancellationToken)
            ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
