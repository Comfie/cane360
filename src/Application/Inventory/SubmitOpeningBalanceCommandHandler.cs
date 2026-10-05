namespace Cane360.Application.Inventory;

public sealed class SubmitOpeningBalanceCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<SubmitOpeningBalanceCommand, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(
        SubmitOpeningBalanceCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        StockReceipt receipt = await inventoryRepository.GetReceiptAsync(
                                   tenant.Id, farm.Id, request.ReceiptId, true, cancellationToken)
                               ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            receipt.SubmitOpeningBalance(request.ExpectedVersion));
        InventoryAudit.Receipt(inventoryRepository, tenant, farm, user, receipt, "SubmittedForApproval",
            timeProvider.GetUtcNow(), receipt.Reason, "Submitted opening balance for grower approval.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
