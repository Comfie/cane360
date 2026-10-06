namespace Cane360.Application.Inventory;

public sealed class CreateInventoryCategoryCommandHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user, TimeProvider clock)
    : IRequestHandler<CreateInventoryCategoryCommand, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(CreateInventoryCategoryCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        if (InventoryAccess.SecurityRole(tenant, InventoryAccess.RequireUserId(user)) != TenantSecurityRoles.FarmManager)
        {
            throw new ForbiddenAccessException();
        }

        IReadOnlyList<InventoryCategory> categories = await inventory.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        if (categories.Any(category => string.Equals(category.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw InventoryAccess.Failure(nameof(request.Code), "Category code is already in use.");
        }

        InventoryCategory category = InventoryAccess.ApplyDomainAction(nameof(request.Name), () =>
            InventoryCategory.Create(tenant.Id, request.Code, request.Name, request.Description, request.DisplayOrder));
        if (categories.Any(existing => existing.NormalizedName == category.NormalizedName))
        {
            throw InventoryAccess.Failure(nameof(request.Name), "Category name is already in use.");
        }

        inventory.Add(category);
        string action = "Created";
        InventoryAudit.Category(inventory, tenant, farm, user, category, action, clock.GetUtcNow(),
            $"Category {category.Code}: {category.Name}; active={category.Active}; display order={category.DisplayOrder}.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Category(category);
    }
}
