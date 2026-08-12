using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record CreateGrowerFarmCommand(
    string GrowerDisplayName,
    string? GrowerPhone,
    string FarmCode,
    string FarmName,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext) : IRequest<FarmSetupDto>;

public sealed class CreateGrowerFarmCommandValidator : AbstractValidator<CreateGrowerFarmCommand>
{
    public CreateGrowerFarmCommandValidator()
    {
        RuleFor(command => command.GrowerDisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.GrowerPhone).MaximumLength(30);
        RuleFor(command => command.FarmCode)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$")
            .WithMessage("Farm code may contain letters, numbers, underscores, and hyphens.");
        RuleFor(command => command.FarmName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Address).NotEmpty().MaximumLength(240);
        RuleFor(command => command.Location).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Tenure).NotEmpty().MaximumLength(80);
        RuleFor(command => command.DeclaredHectares).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(command => command.IrrigationContext).NotEmpty().MaximumLength(160);
    }
}

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
