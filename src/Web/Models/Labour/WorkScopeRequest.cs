namespace Cane360.Web.Models.Labour;

public sealed record WorkScopeRequest(string Type, int? StartLine, int? EndLine, string? SectionName);
