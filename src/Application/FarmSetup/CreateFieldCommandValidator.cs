using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed class CreateFieldCommandValidator : AbstractValidator<CreateFieldCommand>
{
    private static readonly string[] ReportingSources = ["Declared", "Mapped"];

    public CreateFieldCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$")
            .WithMessage("Field code may contain letters, numbers, underscores, and hyphens.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.DeclaredHectares).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(command => command.MappedHectares)
            .GreaterThan(0)
            .LessThanOrEqualTo(100_000)
            .When(command => command.MappedHectares.HasValue);
        RuleFor(command => command.ReportingAreaSource)
            .Must(source => ReportingSources.Contains(source, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Reporting area source must be Declared or Mapped.");
        RuleFor(command => command.MappedHectares)
            .NotNull()
            .When(command => string.Equals(command.ReportingAreaSource, "Mapped", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Mapped hectares are required when mapped area is selected for reporting.");
        RuleFor(command => command.IrrigationMethod).NotEmpty().MaximumLength(100);
        RuleFor(command => command.SoilNotes).MaximumLength(500);
    }
}
