namespace Cane360.Application.Activities;

public sealed record EvidenceLinkDto(
    Guid Id,
    string Role,
    string SourceSheetReference,
    string CapturedDate,
    string RecordedAt,
    string RecordedBy);
