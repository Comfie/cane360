namespace Cane360.Application.Inventory;

public sealed class GetUnitsOfMeasureQueryHandler(
    IFarmSetupRepository farms,
    IInventoryRepository inventory,
    IUser user)
    : IRequestHandler<GetUnitsOfMeasureQuery, IReadOnlyList<UnitOfMeasureDto>>
{
    public async Task<IReadOnlyList<UnitOfMeasureDto>> Handle(
        GetUnitsOfMeasureQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        IReadOnlyList<UnitOfMeasure> units = await inventory.GetUnitsAsync(tenant.Id, false, cancellationToken);
        return units.OrderBy(unit => unit.Code).Select(InventoryMapper.Unit).ToArray();
    }
}
