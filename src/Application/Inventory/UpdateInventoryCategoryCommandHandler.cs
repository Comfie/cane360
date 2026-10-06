namespace Cane360.Application.Inventory;

public sealed class UpdateInventoryCategoryCommandHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user, TimeProvider clock)
    : IRequestHandler<UpdateInventoryCategoryCommand, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(UpdateInventoryCategoryCommand request, CancellationToken cancellationToken)
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

        IReadOnlyList<InventoryCategory> categories = await inventory.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        if (categories.Any(existing => existing.Id != category.Id && existing.NormalizedName == request.Name.Trim().ToUpperInvariant()))
        {
            throw InventoryAccess.Failure(nameof(request.Name), "Category name is already in use.");
        }

        InventoryAccess.ApplyDomainAction(nameof(request.Name), () =>
            category.Update(request.Name, request.Description, request.DisplayOrder, request.ExpectedVersion));
        string action = "Updated";
        InventoryAudit.Category(inventory, tenant, farm, user, category, action, clock.GetUtcNow(),
            $"Category {category.Code}: {category.Name}; active={category.Active}; display order={category.DisplayOrder}.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Category(category);
    }
}
