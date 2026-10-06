namespace Cane360.Application.Inventory;

public sealed class SetInventoryCategoryActiveCommandHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user, TimeProvider clock)
    : IRequestHandler<SetInventoryCategoryActiveCommand, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(SetInventoryCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        if (InventoryAccess.SecurityRole(tenant, InventoryAccess.RequireUserId(user)) != TenantSecurityRoles.FarmManager)
        {
            throw new ForbiddenAccessException();
        }

        InventoryCategory category = await inventory.GetCategoryAsync(tenant.Id, request.CategoryId, true, cancellationToken)
            ?? throw new NotFoundException(request.CategoryId.ToString(), "Inventory category");
        if (category.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This inventory category changed after it was loaded.");
        }

        if (category.Active == request.Active)
        {
            return InventoryMapper.Category(category);
        }

        category.SetActive(request.Active, request.ExpectedVersion);
        string action = request.Active ? "Activated" : "Deactivated";
        InventoryAudit.Category(inventory, tenant, farm, user, category, action, clock.GetUtcNow(),
            $"Category {category.Code}: {category.Name}; active={category.Active}; display order={category.DisplayOrder}.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Category(category);
    }
}
