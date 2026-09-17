using Cane360.Application.Common.Interfaces;
using MediatR;

namespace Cane360.Application.Session;

public sealed class GetSessionQueryHandler(IFarmSetupRepository farms, IUser user)
    : IRequestHandler<GetSessionQuery, SessionSummaryDto>
{
    public async Task<SessionSummaryDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var summary = await farms.GetSessionSummaryForUserAsync(userId, cancellationToken);
        if (summary is null) return new SessionSummaryDto(false, null, null, null);
        return new SessionSummaryDto(true, summary.SecurityRole, summary.TenantCode, summary.FarmName);
    }
}
