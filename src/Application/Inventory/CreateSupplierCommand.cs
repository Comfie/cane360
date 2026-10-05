namespace Cane360.Application.Inventory;

public sealed record CreateSupplierCommand(
    string Code,
    string Name,
    string? Contact) : IRequest<SupplierDto>;
