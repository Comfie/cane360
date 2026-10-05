namespace Cane360.Application.Inventory;

public sealed class ArchiveSupplierCommandHandler(
    IFarmSetupRepository farms,
    IInventoryRepository inventory,
    IUser user,
    TimeProvider clock) : IRequestHandler<ArchiveSupplierCommand, SupplierDto>
{
    public async Task<SupplierDto> Handle(
        ArchiveSupplierCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farms, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        Supplier supplier =
            await inventory.GetSupplierAsync(tenant.Id, farm.Id, request.SupplierId, true, cancellationToken)
            ?? throw new NotFoundException(request.SupplierId.ToString(), "Supplier");
        if (supplier.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This supplier changed after it was loaded. Refresh and try again.");
        }

        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion),
            () => supplier.Archive(request.ExpectedVersion));
        InventoryAudit.Supplier(inventory, tenant, farm, user, supplier, "Archived", clock.GetUtcNow(),
            $"Archived supplier {supplier.Code}; historical receipts retain their original supplier.");
        await inventory.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Supplier(supplier);
    }
}
