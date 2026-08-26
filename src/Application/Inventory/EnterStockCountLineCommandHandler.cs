using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class EnterStockCountLineCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<EnterStockCountLineCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(EnterStockCountLineCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, command.StockCountId, true, cancellationToken) ?? throw new NotFoundException(command.StockCountId.ToString(), "Stock count");
        if (count.Status != StockCountStatus.InProgress) throw new ConflictException("Count lines are editable only while the count is in progress.");
        var line = count.Lines.SingleOrDefault(line => line.Id == command.StockCountLineId) ?? throw new NotFoundException(command.StockCountLineId.ToString(), "Stock count line");
        var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => line.Enter(command.CountedQuantity, command.Notes, now, userId, command.ExpectedVersion));
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "CountEntryCorrected", now, command.Notes, "Entered or corrected an in-progress count line.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); return InventoryMapper.Count(count);
    }
}
