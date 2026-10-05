namespace Cane360.Application.Labour;

public sealed record RecordAttendanceCommand(
    DateOnly WorkDate,
    string? LateEntryReason,
    IReadOnlyList<AttendanceEntryCommand> Entries) : IRequest<AttendanceRegisterDto>;
