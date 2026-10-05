namespace Cane360.Application.FarmSetup;

public sealed class UpdateFarmInformationCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IWorkerSensitiveDataProtector protector,
    TimeProvider clock) : IRequestHandler<UpdateFarmInformationCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        UpdateFarmInformationCommand request,
        CancellationToken cancellationToken)
    {
        string userId = FarmSetupValidation.RequireUserId(user);
        Tenant tenant = await repository.GetTenantForUserAsync(userId, true, cancellationToken)
                        ?? throw FarmSetupValidation.Failure(nameof(request.FarmName),
                            "Create your farm before editing its details.");
        Farm farm = tenant.ActiveFarm
                    ?? throw FarmSetupValidation.Failure(nameof(request.FarmName),
                        "No active farm is available to edit.");

        FarmOwnerProfileInput? profile = request.OwnerProfile;
        if (request.UpdateFarmModel)
        {
            FarmModel? model = request.FarmModelId is null ? null :
                (await repository.GetFarmModelsAsync(tenant.Id, false, cancellationToken))
                .SingleOrDefault(item => item.Id == request.FarmModelId)
                ?? throw FarmSetupValidation.Failure(nameof(request.FarmModelId), "Farm Model is unavailable.");
            try { farm.AssignModel(model); }
            catch (InvalidOperationException exception)
            { throw FarmSetupValidation.Failure(nameof(request.FarmModelId), exception.Message); }
        }
        FarmOwnerProfileUpdater.Apply(tenant, profile, repository, user, protector, clock);
        FarmProfileAudit.Add(repository, tenant, user, clock, "ProfileUpdated",
            "Farm Owner identity, association, contact, status and farm details updated; values omitted.");
        if (request.UpdateFarmModel)
        {
            FarmProfileAudit.Add(repository, tenant, user, clock, "FarmModelAssigned",
                "Farm Model classification updated.", nameof(Farm), farm.Id);
        }

        tenant.GrowerProfile.Update(request.GrowerDisplayName, request.GrowerPhone);
        farm.UpdateDetails(
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext);
        await repository.SaveChangesAsync(cancellationToken);
        return FarmSetupMapper.Map(tenant);
    }
}
