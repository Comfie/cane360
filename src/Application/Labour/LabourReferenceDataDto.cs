using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record LabourReferenceDataDto(
    IReadOnlyList<LabourFieldDto> Fields,
    IReadOnlyList<LabourActivityDto> Activities,
    IReadOnlyList<LabourPersonDto> Supervisors);
