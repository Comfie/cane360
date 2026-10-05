namespace Cane360.Application.Labour;

public sealed record GetAttendanceRegisterQuery(DateOnly WorkDate) : IRequest<AttendanceRegisterDto>;
