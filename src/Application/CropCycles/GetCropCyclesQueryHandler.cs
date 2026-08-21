namespace Cane360.Application.CropCycles;

public sealed class GetCropCyclesQueryHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<GetCropCyclesQuery, CropCycleCollectionDto>
{
    public async Task<CropCycleCollectionDto> Handle(
        GetCropCyclesQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, false, cancellationToken);
        var field = CropCycleAccess.RequireField(tenant, request.FieldId);
        return CropCycleMapper.MapCollection(field);
    }
}
