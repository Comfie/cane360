using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record RecordAttendanceCommand(
    DateOnly WorkDate,
    string? LateEntryReason,
    IReadOnlyList<AttendanceEntryCommand> Entries) : IRequest<AttendanceRegisterDto>;
