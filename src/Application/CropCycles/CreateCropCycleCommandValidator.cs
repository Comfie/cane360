using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CreateCropCycleCommandValidator : AbstractValidator<CreateCropCycleCommand>
{
    private static readonly string[] CycleTypes = ["PlantCane", "Ratoon"];

    public CreateCropCycleCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropVarietyId).NotEmpty();
        RuleFor(command => command.CycleType)
            .Must(type => CycleTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Cycle type must be PlantCane or Ratoon.");
        RuleFor(command => command.RatoonNumber)
            .NotNull()
            .GreaterThanOrEqualTo(1)
            .When(command => string.Equals(command.CycleType, "Ratoon", StringComparison.OrdinalIgnoreCase))
            .WithMessage("A ratoon number is required for a ratoon crop cycle.");
        RuleFor(command => command.RatoonNumber)
            .Null()
            .When(command => string.Equals(command.CycleType, "PlantCane", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Plant cane cannot carry a ratoon number.");
        RuleFor(command => command.ExpectedHarvestStart)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("Expected harvest must not begin before the crop cycle starts.");
        RuleFor(command => command.ExpectedHarvestEnd)
            .GreaterThanOrEqualTo(command => command.ExpectedHarvestStart)
            .WithMessage("Expected harvest end must be on or after its start.");
        RuleFor(command => command.ExpectedYieldTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}
