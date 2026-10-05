namespace Cane360.Application.Inventory;

public sealed class GetStockReceiptQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockReceiptQuery, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(GetStockReceiptQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        StockReceipt receipt = await inventoryRepository.GetReceiptAsync(
                                   tenant.Id, farm.Id, request.ReceiptId, false, cancellationToken)
                               ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
