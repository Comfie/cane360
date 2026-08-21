using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class GetCropVarietiesQueryHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<GetCropVarietiesQuery, IReadOnlyList<CropVarietyDto>>
{
    public async Task<IReadOnlyList<CropVarietyDto>> Handle(
        GetCropVarietiesQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, false, cancellationToken);

        return tenant.CropVarieties
            .Where(variety => variety.Status == RecordStatus.Active)
            .OrderBy(variety => variety.Code)
            .Select(variety => new CropVarietyDto(variety.Id, variety.Code, variety.Name))
            .ToArray();
    }
}
