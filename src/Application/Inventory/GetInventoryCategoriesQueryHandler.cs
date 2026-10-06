namespace Cane360.Application.Inventory;

public sealed class GetInventoryCategoriesQueryHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user)
    : IRequestHandler<GetInventoryCategoriesQuery, IReadOnlyList<InventoryCategoryDto>>
{
    public async Task<IReadOnlyList<InventoryCategoryDto>> Handle(GetInventoryCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        string userId = InventoryAccess.RequireUserId(user);
        Tenant tenant = await farms.GetTenantForOperationalUserAsync(userId, false, cancellationToken)
            ?? throw new NotFoundException(userId, "Active tenant membership");
        return (await inventory.GetCategoriesAsync(tenant.Id, false, cancellationToken))
            .Select(InventoryMapper.Category).ToArray();
    }
}
