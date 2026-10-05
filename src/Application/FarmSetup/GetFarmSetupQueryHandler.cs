namespace Cane360.Application.FarmSetup;

public sealed class GetFarmSetupQueryHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<GetFarmSetupQuery, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        GetFarmSetupQuery request,
        CancellationToken cancellationToken)
    {
        string userId = FarmSetupValidation.RequireUserId(user);
        Tenant? tenant = await repository.GetTenantWorkspaceForUserAsync(userId, cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
