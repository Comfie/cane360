using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class ArchiveUnitOfMeasureCommandHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user,
    TimeProvider clock) : IRequestHandler<ArchiveUnitOfMeasureCommand, UnitOfMeasureDto>
{
    public async Task<UnitOfMeasureDto> Handle(
        ArchiveUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);

        var unit = await inventory.GetUnitAsync(tenant.Id, request.UnitId, true, cancellationToken)
            ?? throw new NotFoundException(request.UnitId.ToString(), "Unit of measure");
        if (unit.Version != request.ExpectedVersion)
            throw new ConflictException("This unit changed after it was loaded. Refresh and try again.");
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => unit.Archive(request.ExpectedVersion));
        InventoryAudit.Unit(inventory, tenant, farm, user, unit, "Archived", clock.GetUtcNow(),
            $"Archived stock unit {unit.Code}; historical stock and work retain their original unit.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Unit(unit);
    }
}
