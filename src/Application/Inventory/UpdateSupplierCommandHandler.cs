using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class UpdateSupplierCommandHandler(IFarmSetupRepository farms,
    IInventoryRepository inventory, IUser user, TimeProvider clock)
    : IRequestHandler<UpdateSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(UpdateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var supplier = await inventory.GetSupplierAsync(tenant.Id, farm.Id, request.SupplierId, true, cancellationToken)
            ?? throw new NotFoundException(request.SupplierId.ToString(), "Supplier");
        if (supplier.Version != request.ExpectedVersion)
            throw new ConflictException("This supplier changed after it was loaded. Refresh and try again.");
        InventoryAccess.ApplyDomainAction(nameof(request.Code), () =>
            supplier.Update(request.Code, request.Name, request.Contact, request.ExpectedVersion));
        InventoryAudit.Supplier(inventory, tenant, farm, user, supplier, "Updated",
            clock.GetUtcNow(), $"Supplier {supplier.Code} details changed.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Supplier(supplier);
    }
}
