namespace Cane360.Application.Labour;

public sealed record GetWorkRecordsQuery(DateOnly? WorkDate, Guid? WorkerId, Guid? ActivityId)
    : IRequest<IReadOnlyList<WorkRecordDto>>;
