namespace Cane360.Application.FarmSetup;

public sealed class CreateGrowerFarmCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IWorkerSensitiveDataProtector protector,
    TimeProvider clock) : IRequestHandler<CreateGrowerFarmCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        CreateGrowerFarmCommand request,
        CancellationToken cancellationToken)
    {
        string userId = FarmSetupValidation.RequireUserId(user);
        Tenant? existingTenant = await repository.GetTenantForUserAsync(userId, false, cancellationToken);

        if (existingTenant is not null)
        {
            throw FarmSetupValidation.Failure(
                nameof(CreateGrowerFarmCommand.FarmName),
                "This grower already has an active farm.");
        }

        Tenant tenant = Tenant.CreateForGrower(userId, request.GrowerDisplayName, request.GrowerPhone);
        tenant.CreateFarm(
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext);

        FarmOwnerProfileUpdater.Apply(tenant, request.OwnerProfile, repository, user, protector, clock);
        FarmProfileAudit.Add(repository, tenant, user, clock, "ProfileCreated",
            "Farm Owner profile created; identity and contact values omitted.");
        repository.Add(tenant);
        await repository.SaveChangesAsync(cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
