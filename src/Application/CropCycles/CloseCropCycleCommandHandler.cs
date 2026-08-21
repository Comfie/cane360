using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CloseCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CloseCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(CloseCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            cycle.Close(timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}
