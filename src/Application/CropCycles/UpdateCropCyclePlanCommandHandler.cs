using Microsoft.Extensions.Options;

namespace Cane360.Application.CropCycles;

public sealed class UpdateCropCyclePlanCommandHandler(IFarmSetupRepository repository, IUser user,
    TimeProvider timeProvider, IOptions<CropMaturityOptions> maturityOptions)
    : IRequestHandler<UpdateCropCyclePlanCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(UpdateCropCyclePlanCommand request, CancellationToken cancellationToken)
    {
        (Field field, CropCycle cycle, string userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        DateOnly maturity = maturityOptions.Value.Calculate(request.StartDate)!.Value;
        CropCycleAccess.ApplyDomainAction(nameof(request.StartDate), () => cycle.UpdateDraftPlan(
            request.StartDate, request.ExpectedHarvestStart ?? maturity, request.ExpectedHarvestEnd ?? maturity,
            request.ExpectedYieldTonnes, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
    }
}
