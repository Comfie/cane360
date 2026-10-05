namespace Cane360.Application.CropCycles;

public sealed class UpdateActualYieldCommandHandler(IFarmSetupRepository repository, IUser user,
    TimeProvider timeProvider) : IRequestHandler<UpdateActualYieldCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(UpdateActualYieldCommand request, CancellationToken cancellationToken)
    {
        (Field field, CropCycle cycle, string userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.ActualTonnes), () =>
            cycle.UpdateActualYield(request.ActualTonnes, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
    }
}
