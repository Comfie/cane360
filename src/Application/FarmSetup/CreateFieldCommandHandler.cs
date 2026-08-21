using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed class CreateFieldCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<CreateFieldCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        CreateFieldCommand request,
        CancellationToken cancellationToken)
    {
        var userId = FarmSetupValidation.RequireUserId(user);
        var tenant = await repository.GetTenantForUserAsync(userId, true, cancellationToken);
        var farm = tenant?.ActiveFarm ?? throw new NotFoundException(userId, "Active farm");

        if (farm.Fields.Any(field =>
                field.Status == RecordStatus.Active &&
                field.Code.Equals(request.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw FarmSetupValidation.Failure(
                nameof(CreateFieldCommand.Code),
                "This field code is already in use on the farm.");
        }

        farm.AddField(
            request.Code,
            request.Name,
            request.DeclaredHectares,
            request.MappedHectares,
            Enum.Parse<ReportingAreaSource>(request.ReportingAreaSource, true),
            request.IrrigationMethod,
            request.SoilNotes);

        await repository.SaveChangesAsync(cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
