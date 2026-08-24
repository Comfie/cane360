using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record GetWorkRecordsQuery(DateOnly? WorkDate, Guid? WorkerId, Guid? ActivityId) : IRequest<IReadOnlyList<WorkRecordDto>>;
