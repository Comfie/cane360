using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateSupplierCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var supplier = Supplier.Create(tenant.Id, farm.Id, request.Code, request.Name, request.Contact);
        inventoryRepository.Add(supplier);
        InventoryAudit.Supplier(inventoryRepository, tenant, farm, user, supplier, "Created", timeProvider.GetUtcNow(),
            $"Created supplier {supplier.Code}.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Supplier(supplier);
    }
}
