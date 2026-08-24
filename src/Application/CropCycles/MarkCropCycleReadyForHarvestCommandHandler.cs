using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class MarkCropCycleReadyForHarvestCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<MarkCropCycleReadyForHarvestCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(MarkCropCycleReadyForHarvestCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            cycle.MarkReadyForHarvest(timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}
