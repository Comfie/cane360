using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class UnarchiveSupplierCommandHandler(
    IFarmSetupRepository farms, IInventoryRepository inventory, IUser user,
    TimeProvider clock) : IRequestHandler<UnarchiveSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(
        UnarchiveSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var supplier = await inventory.GetSupplierAsync(tenant.Id, farm.Id, request.SupplierId, true, cancellationToken)
            ?? throw new NotFoundException(request.SupplierId.ToString(), "Supplier");
        if (supplier.Version != request.ExpectedVersion)
            throw new ConflictException("This supplier changed after it was loaded. Refresh and try again.");
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => supplier.Unarchive(request.ExpectedVersion));
        InventoryAudit.Supplier(inventory, tenant, farm, user, supplier, "Unarchived", clock.GetUtcNow(),
            $"Unarchived supplier {supplier.Code}.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Supplier(supplier);
    }
}
