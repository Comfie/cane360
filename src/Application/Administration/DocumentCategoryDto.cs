namespace Cane360.Application.Administration;

public sealed record DocumentCategoryDto(Guid Id, string Code, string Name,
    string? Description, bool Active, long Version);
