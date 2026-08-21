using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CreateCropVarietyCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<CreateCropVarietyCommand, CropVarietyDto>
{
    public async Task<CropVarietyDto> Handle(
        CreateCropVarietyCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, true, cancellationToken);
        CropVariety? variety = null;

        CropCycleAccess.ApplyDomainAction(nameof(request.Code), () =>
            variety = tenant.AddCropVariety(request.Code, request.Name));

        await repository.SaveChangesAsync(cancellationToken);
        return new CropVarietyDto(variety!.Id, variety.Code, variety.Name);
    }
}
