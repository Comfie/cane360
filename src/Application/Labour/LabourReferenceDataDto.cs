namespace Cane360.Application.Labour;

public sealed record LabourReferenceDataDto(
    IReadOnlyList<LabourFieldDto> Fields,
    IReadOnlyList<LabourActivityDto> Activities,
    IReadOnlyList<LabourPersonDto> Supervisors);
