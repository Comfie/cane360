namespace Cane360.Application.Labour;

public sealed record VerifyWorkRecordCommand(Guid WorkRecordId, Guid SupervisorPersonId, long ExpectedVersion)
    : IRequest<WorkRecordDto>;
