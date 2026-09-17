namespace Cane360.Application.Inventory;

public sealed record ArchiveSupplierCommand(Guid SupplierId, long ExpectedVersion)
    : IRequest<SupplierDto>;
