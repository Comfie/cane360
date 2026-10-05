namespace Cane360.Application.Labour;

public sealed record WorkScopeCommand(string Type, int? StartLine, int? EndLine, string? SectionName);
