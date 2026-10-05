namespace Cane360.Application.FarmSetup;

public sealed class GetFarmModelsQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetFarmModelsQuery, IReadOnlyList<FarmModelDto>>
{
    public async Task<IReadOnlyList<FarmModelDto>> Handle(GetFarmModelsQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await repository.GetTenantWorkspaceForUserAsync(FarmSetupValidation.RequireUserId(user), cancellationToken)
            ?? throw new ForbiddenAccessException();
        return (await repository.GetFarmModelsAsync(tenant.Id, false, cancellationToken))
            .Select(model => new FarmModelDto(model.Id, model.Code, model.Name, model.Active, model.Version)).ToArray();
    }
}
