using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CancelCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CancelCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(CancelCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.Reason), () =>
            cycle.Cancel(request.Reason, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}
