using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;

namespace Cane360.Application.Inventory;

public sealed class DecideStockAdjustmentCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<DecideStockAdjustmentCommand, StockAdjustmentDto>
{
    public async Task<StockAdjustmentDto> Handle(DecideStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrower(tenant, userId);
        var candidate = await inventoryRepository.GetStockAdjustmentAsync(tenant.Id, farm.Id, command.StockAdjustmentId, false, cancellationToken) ?? throw new NotFoundException(command.StockAdjustmentId.ToString(), "Stock adjustment");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.LockStockAdjustmentAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        var adjustment = await inventoryRepository.GetStockAdjustmentAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock adjustment");
        if (adjustment.Version != command.ExpectedVersion) throw new ConflictException("This adjustment changed after it was loaded. Refresh and try again.");
        var existing = await inventoryRepository.GetStockAdjustmentApprovalAsync(adjustment.Id, command.ExpectedVersion, cancellationToken); if (existing?.IdempotencyKey == command.IdempotencyKey) return InventoryMapper.Adjustment(adjustment); if (existing is not null) throw new ConflictException("This adjustment version has already been decided.");
        var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => adjustment.Decide(command.Outcome, command.ExpectedVersion));
        inventoryRepository.Add(ApprovalDecision.CreateStockAdjustmentDecision(tenant.Id, farm.Id, adjustment.Id, command.ExpectedVersion, command.Outcome, userId, TenantSecurityRoles.Grower, now, command.Reason, command.IdempotencyKey));
        InventoryAudit.Adjustment(inventoryRepository, tenant, farm, user, adjustment, command.Outcome.ToString(), now, command.Reason, "Grower decided the exact submitted stock-adjustment version.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return InventoryMapper.Adjustment(adjustment);
    }
}
