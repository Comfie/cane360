namespace Cane360.Application.Labour;

public sealed record ConfirmWorkRecordCommand(Guid WorkRecordId, long ExpectedVersion) : IRequest<WorkRecordDto>;
