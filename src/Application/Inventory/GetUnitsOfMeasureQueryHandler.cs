using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class GetUnitsOfMeasureQueryHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user)
    : IRequestHandler<GetUnitsOfMeasureQuery, IReadOnlyList<UnitOfMeasureDto>>
{
    public async Task<IReadOnlyList<UnitOfMeasureDto>> Handle(
        GetUnitsOfMeasureQuery request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        var units = await inventory.GetUnitsAsync(tenant.Id, false, cancellationToken);
        return units.OrderBy(unit => unit.Code).Select(InventoryMapper.Unit).ToArray();
    }
}
