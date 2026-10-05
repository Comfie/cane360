namespace Cane360.Application.Labour;

public sealed class CreateWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user,
    TimeProvider timeProvider)
    : IRequestHandler<CreateWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CreateWorkRecordCommand request, CancellationToken cancellationToken)
    {
        return await WorkRecordActions.CreateAsync(farmRepository, labourRepository, user, timeProvider, request, null,
            cancellationToken);
    }
}
