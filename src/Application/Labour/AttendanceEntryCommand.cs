namespace Cane360.Application.Labour;

public sealed record AttendanceEntryCommand(
    Guid WorkerId,
    string Status,
    Guid? FieldId,
    long? ExpectedVersion);
