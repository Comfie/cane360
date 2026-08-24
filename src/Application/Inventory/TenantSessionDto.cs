namespace Cane360.Application.Inventory;

public sealed record TenantSessionDto(
    Guid TenantId,
    Guid FarmId,
    string SecurityRole,
    Guid? PersonId,
    string? PersonName);
