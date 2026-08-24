using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateStockReceiptCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateStockReceiptCommand, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(
        CreateStockReceiptCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var type = Enum.Parse<StockReceiptType>(request.ReceiptType, true);
        if (request.SupplierId.HasValue && await inventoryRepository.GetSupplierAsync(
                tenant.Id, farm.Id, request.SupplierId.Value, false, cancellationToken) is not
            { Status: InventoryRecordStatus.Active })
        {
            throw new NotFoundException(request.SupplierId.Value.ToString(), "Supplier");
        }
        if (request.ReceivedByPersonId.HasValue && farm.Persons.All(person => person.Id != request.ReceivedByPersonId))
        {
            throw new NotFoundException(request.ReceivedByPersonId.Value.ToString(), "Receiving person");
        }
        var now = timeProvider.GetUtcNow();
        var delay = InventoryAccess.EntryDelay(request.ReceiptDate, now);
        var receipt = InventoryAccess.ApplyDomainAction(nameof(request.ReceiptType), () => StockReceipt.Create(
            tenant.Id, farm.Id, farm.Store.Id, type, request.SupplierId, request.ReceiptDate,
            request.ReceivedByPersonId, request.SourceReference, request.Reason, request.LateEntryReason, delay));
        foreach (var lineRequest in request.Lines)
        {
            var item = await inventoryRepository.GetItemAsync(
                tenant.Id, farm.Id, lineRequest.InventoryItemId, false, cancellationToken)
                ?? throw new NotFoundException(lineRequest.InventoryItemId.ToString(), "Inventory item");
            InventoryLot? lot = null;
            if (lineRequest.InventoryLotId.HasValue)
            {
                lot = await inventoryRepository.GetLotAsync(
                    tenant.Id, farm.Id, lineRequest.InventoryLotId.Value, false, cancellationToken)
                    ?? throw new NotFoundException(lineRequest.InventoryLotId.Value.ToString(), "Inventory lot");
            }
            InventoryAccess.ApplyDomainAction(nameof(request.Lines), () => receipt.AddLine(
                item, lot, lineRequest.Quantity, lineRequest.UnitCostUsd, receipt.Version));
        }
        inventoryRepository.Add(receipt);
        InventoryAudit.Receipt(inventoryRepository, tenant, farm, user, receipt, "Created", now,
            request.Reason, $"Created draft {receipt.ReceiptType} receipt with {receipt.Lines.Count} line(s).");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
