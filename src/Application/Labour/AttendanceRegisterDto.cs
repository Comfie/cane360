using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record AttendanceRegisterDto(
    DateOnly WorkDate,
    IReadOnlyList<AttendanceRowDto> Rows,
    IReadOnlyList<LabourFieldDto> Fields);
