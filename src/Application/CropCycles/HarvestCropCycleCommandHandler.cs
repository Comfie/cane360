using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class HarvestCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<HarvestCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(HarvestCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        CropCycleAccess.ApplyDomainAction(nameof(request.HarvestDate), () =>
            cycle.RecordHarvest(request.HarvestDate, request.ActualTonnes, today, now, userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}
