namespace Cane360.Application.Inventory;

public sealed record UpdateSupplierCommand(Guid SupplierId, string Code, string Name,
    string? Contact, long ExpectedVersion) : IRequest<SupplierDto>;
