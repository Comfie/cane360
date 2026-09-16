using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class RenameUnitOfMeasureCommandHandler(IFarmSetupRepository farms,
    IInventoryRepository inventory, IUser user, TimeProvider clock)
    : IRequestHandler<RenameUnitOfMeasureCommand, UnitOfMeasureDto>
{
    public async Task<UnitOfMeasureDto> Handle(RenameUnitOfMeasureCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var unit = await inventory.GetUnitAsync(tenant.Id, request.UnitId, true, cancellationToken)
            ?? throw new NotFoundException(request.UnitId.ToString(), "Unit of measure");
        if (unit.Version != request.ExpectedVersion)
            throw new ConflictException("This unit changed after it was loaded.");
        InventoryAccess.ApplyDomainAction(nameof(request.Name), () =>
            unit.Rename(request.Name, request.ExpectedVersion));
        InventoryAudit.Unit(inventory, tenant, farm, user, unit, "Renamed",
            clock.GetUtcNow(), $"Unit {unit.Code} renamed; historical stock units are unchanged.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Unit(unit);
    }
}
