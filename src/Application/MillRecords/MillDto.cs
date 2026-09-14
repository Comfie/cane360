namespace Cane360.Application.MillRecords;

public sealed record MillDto(Guid Id, string Code, string Name, string? Location,
    bool Active, DateTimeOffset CreatedAt, long Version);
