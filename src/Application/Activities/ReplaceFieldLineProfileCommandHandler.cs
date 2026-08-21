using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class ReplaceFieldLineProfileCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<ReplaceFieldLineProfileCommand, FieldLineProfileDto>
{
    public async Task<FieldLineProfileDto> Handle(ReplaceFieldLineProfileCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var field = ActivityAccess.RequireField(ActivityAccess.RequireFarm(tenant), request.FieldId);
        if (field.CurrentLineProfile?.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This line profile changed after it was loaded. Refresh and try again.");
        }
        FieldLineProfile? profile = null;
        ActivityAccess.ApplyDomainAction(nameof(request.EffectiveFrom), () => profile = field.ReplaceLineProfile(
            request.StandardLineLengthMetres, request.EstimatedLineCount, request.NumberingScheme, request.EffectiveFrom));
        await repository.SaveChangesAsync(cancellationToken);
        return GetFieldLineProfileQueryHandler.Map(profile!);
    }
}
