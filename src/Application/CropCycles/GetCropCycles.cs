namespace Cane360.Application.CropCycles;

public sealed record GetCropCyclesQuery(Guid FieldId) : IRequest<CropCycleCollectionDto>;

public sealed record GetCropCycleDetailsQuery(Guid FieldId, Guid CropCycleId) : IRequest<CropCycleDetailsDto>;

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

public sealed class GetCropCycleDetailsQueryHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService) : IRequestHandler<GetCropCycleDetailsQuery, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(
        GetCropCycleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, false, cancellationToken);
        var field = CropCycleAccess.RequireField(tenant, request.FieldId);
        var cropCycle = CropCycleAccess.RequireCycle(field, request.CropCycleId);
        return await CropCycleMapper.MapDetailsAsync(field, cropCycle, tenant.ActiveFarm!, identityService);
    }
}
