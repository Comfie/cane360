using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed class CreateGrowerFarmCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<CreateGrowerFarmCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        CreateGrowerFarmCommand request,
        CancellationToken cancellationToken)
    {
        var userId = FarmSetupValidation.RequireUserId(user);
        var existingTenant = await repository.GetTenantForUserAsync(userId, false, cancellationToken);

        if (existingTenant is not null)
        {
            throw FarmSetupValidation.Failure(
                nameof(CreateGrowerFarmCommand.FarmName),
                "This grower already has an active farm.");
        }

        var tenant = Tenant.CreateForGrower(userId, request.GrowerDisplayName, request.GrowerPhone);
        tenant.CreateFarm(
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext);

        repository.Add(tenant);
        await repository.SaveChangesAsync(cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
