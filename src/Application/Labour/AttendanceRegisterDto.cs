namespace Cane360.Application.Labour;

public sealed record AttendanceRegisterDto(
    DateOnly WorkDate,
    IReadOnlyList<AttendanceRowDto> Rows,
    IReadOnlyList<LabourFieldDto> Fields);
