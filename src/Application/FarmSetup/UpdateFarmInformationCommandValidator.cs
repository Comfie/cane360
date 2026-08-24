namespace Cane360.Application.FarmSetup;

public sealed class UpdateFarmInformationCommandValidator : AbstractValidator<UpdateFarmInformationCommand>
{
    public UpdateFarmInformationCommandValidator()
    {
        RuleFor(command => command.GrowerDisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.GrowerPhone).MaximumLength(30);
        RuleFor(command => command.FarmCode).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");
        RuleFor(command => command.FarmName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Address).NotEmpty().MaximumLength(240);
        RuleFor(command => command.Location).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Tenure).NotEmpty().MaximumLength(120);
        RuleFor(command => command.DeclaredHectares).GreaterThan(0).LessThanOrEqualTo(100000);
        RuleFor(command => command.IrrigationContext).NotEmpty().MaximumLength(160);
    }
}
