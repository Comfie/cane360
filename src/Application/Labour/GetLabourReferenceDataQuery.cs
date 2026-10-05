namespace Cane360.Application.Labour;

public sealed record GetLabourReferenceDataQuery(DateOnly WorkDate) : IRequest<LabourReferenceDataDto>;
