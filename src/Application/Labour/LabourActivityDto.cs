using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record LabourActivityDto(Guid Id, Guid ActivityTypeId, string Name, Guid FieldId, DateOnly WorkDate, string QuantityBasis, string Status);
