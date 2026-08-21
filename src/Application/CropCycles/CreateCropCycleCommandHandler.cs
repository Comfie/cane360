using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CreateCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(
        CreateCropCycleCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, true, cancellationToken);
        var field = CropCycleAccess.RequireField(tenant, request.FieldId);
        var variety = tenant.CropVarieties.SingleOrDefault(candidate =>
            candidate.Id == request.CropVarietyId && candidate.Status == RecordStatus.Active)
            ?? throw new NotFoundException(request.CropVarietyId.ToString(), "Active crop variety");
        var userId = CropCycleAccess.RequireUserId(user);
        CropCycle? cropCycle = null;

        CropCycleAccess.ApplyDomainAction(nameof(request.CycleType), () =>
            cropCycle = field.CreateCropCycleDraft(
                Enum.Parse<CropCycleType>(request.CycleType, true),
                request.RatoonNumber,
                variety,
                variety.Name,
                request.StartDate,
                request.ExpectedHarvestStart,
                request.ExpectedHarvestEnd,
                request.ExpectedYieldTonnes,
                timeProvider.GetUtcNow(),
                userId));

        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cropCycle!);
    }
}
