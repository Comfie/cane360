using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record AttendanceEntryCommand(
    Guid WorkerId,
    string Status,
    Guid? FieldId,
    long? ExpectedVersion);
