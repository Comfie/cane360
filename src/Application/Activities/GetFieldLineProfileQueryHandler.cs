using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class GetFieldLineProfileQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetFieldLineProfileQuery, FieldLineProfileDto?>
{
    public async Task<FieldLineProfileDto?> Handle(GetFieldLineProfileQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var field = ActivityAccess.RequireField(ActivityAccess.RequireFarm(tenant), request.FieldId);
        return field.CurrentLineProfile is null ? null : Map(field.CurrentLineProfile);
    }

    internal static FieldLineProfileDto Map(FieldLineProfile profile) => new(
        profile.Id,
        profile.FieldId,
        profile.StandardLineLengthMetres,
        profile.EstimatedLineCount,
        profile.NumberingScheme,
        profile.EffectiveFrom.ToString("yyyy-MM-dd"),
        profile.EffectiveTo?.ToString("yyyy-MM-dd"),
        profile.Version);
}
