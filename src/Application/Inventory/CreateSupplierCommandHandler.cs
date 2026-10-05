namespace Cane360.Application.Inventory;

public sealed class CreateSupplierCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        Supplier supplier = Supplier.Create(tenant.Id, farm.Id, request.Code, request.Name, request.Contact);
        inventoryRepository.Add(supplier);
        InventoryAudit.Supplier(inventoryRepository, tenant, farm, user, supplier, "Created", timeProvider.GetUtcNow(),
            $"Created supplier {supplier.Code}.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Supplier(supplier);
    }
}
