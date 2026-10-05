namespace Cane360.Application.Activities;

public sealed class GetFieldLineProfileQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetFieldLineProfileQuery, FieldLineProfileDto?>
{
    public async Task<FieldLineProfileDto?> Handle(GetFieldLineProfileQuery request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, false, cancellationToken);
        Field field = ActivityAccess.RequireField(ActivityAccess.RequireFarm(tenant), request.FieldId);
        return field.CurrentLineProfile is null ? null : Map(field.CurrentLineProfile);
    }

    internal static FieldLineProfileDto Map(FieldLineProfile profile)
    {
        return new FieldLineProfileDto(
            profile.Id,
            profile.FieldId,
            profile.StandardLineLengthMetres,
            profile.EstimatedLineCount,
            profile.NumberingScheme,
            profile.EffectiveFrom.ToString("yyyy-MM-dd"),
            profile.EffectiveTo?.ToString("yyyy-MM-dd"),
            profile.Version);
    }
}
