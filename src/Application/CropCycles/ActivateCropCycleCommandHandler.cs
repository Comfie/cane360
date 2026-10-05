namespace Cane360.Application.CropCycles;

public sealed class ActivateCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<ActivateCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(ActivateCropCycleCommand request, CancellationToken cancellationToken)
    {
        (Field field, CropCycle cycle, string userId) = await LoadAsync(repository, user, request.FieldId,
            request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            field.ActivateCropCycle(cycle, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
    }

    internal static async Task<(Field Field, CropCycle Cycle, string UserId)> LoadAsync(
        IFarmSetupRepository repository,
        IUser user,
        Guid fieldId,
        Guid cropCycleId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await CropCycleAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        Field field = CropCycleAccess.RequireField(tenant, fieldId);
        CropCycle cycle = CropCycleAccess.RequireCycle(field, cropCycleId);
        CropCycleAccess.RequireVersion(cycle, expectedVersion);
        return (field, cycle, CropCycleAccess.RequireUserId(user));
    }
}
