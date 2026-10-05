namespace Cane360.Application.FarmSetup;

public sealed class UpdateFarmInformationCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<UpdateFarmInformationCommand, FarmSetupDto>
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
