namespace Cane360.Application.Common.Models;

public sealed record ReportExportContext(string Farm, string Report, string Filters,
    DateTimeOffset GeneratedAt, string Source);
