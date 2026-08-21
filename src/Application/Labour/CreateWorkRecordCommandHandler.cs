using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CreateWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CreateWorkRecordCommand request, CancellationToken cancellationToken) =>
        await WorkRecordActions.CreateAsync(farmRepository, labourRepository, user, timeProvider, request, null, cancellationToken);
}
