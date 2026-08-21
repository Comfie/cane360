namespace Cane360.Application.FarmSetup;

public sealed class GetFarmSetupQueryHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<GetFarmSetupQuery, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        GetFarmSetupQuery request,
        CancellationToken cancellationToken)
    {
        var userId = FarmSetupValidation.RequireUserId(user);
        var tenant = await repository.GetTenantForUserAsync(userId, false, cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
