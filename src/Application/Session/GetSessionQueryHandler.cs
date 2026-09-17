using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using MediatR;

namespace Cane360.Application.Session;

public sealed class GetSessionQueryHandler(IFarmSetupRepository farms, IUser user)
    : IRequestHandler<GetSessionQuery, SessionSummaryDto>
{
    public async Task<SessionSummaryDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var tenant = await farms.GetTenantForUserAsync(userId, false, cancellationToken);
        if (tenant is null) return new SessionSummaryDto(false, null, null, null);
        var membership = tenant.Memberships.Single(m => m.UserId == userId && m.Status == RecordStatus.Active);
        return new SessionSummaryDto(true, membership.SecurityRole, tenant.TenantCode, tenant.ActiveFarm?.Name);
    }
}
