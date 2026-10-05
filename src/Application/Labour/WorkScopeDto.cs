namespace Cane360.Application.Labour;

public sealed record WorkScopeDto(
    string Type,
    Guid ActivityId,
    Guid? FieldLineProfileId,
    int? StartLine,
    int? EndLine,
    string? SectionName);
