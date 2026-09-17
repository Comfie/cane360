namespace Cane360.Application.Inventory;

public sealed record UnarchiveSupplierCommand(Guid SupplierId, long ExpectedVersion)
    : IRequest<SupplierDto>;
