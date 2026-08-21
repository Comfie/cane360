using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

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
