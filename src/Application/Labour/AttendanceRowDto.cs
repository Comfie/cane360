namespace Cane360.Application.Labour;

public sealed record AttendanceRowDto(
    Guid WorkerId,
    string WorkerName,
    string EmploymentType,
    Guid? AttendanceId,
    DateOnly WorkDate,
    string? Status,
    Guid? FieldId,
    string? FieldName,
    int EntryDelayDays,
    long? Version);
