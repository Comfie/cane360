namespace Cane360.Application.Inventory;

public sealed record SupplierDto(
    Guid Id, string Code, string Name, string? Contact, string Status, long Version);
