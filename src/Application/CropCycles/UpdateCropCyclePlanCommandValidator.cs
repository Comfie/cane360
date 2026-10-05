namespace Cane360.Application.CropCycles;

public sealed class UpdateCropCyclePlanCommandValidator : AbstractValidator<UpdateCropCyclePlanCommand>
{
    public UpdateCropCyclePlanCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropCycleId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.StartDate).NotEmpty().Must(date => date.Year <= 9989);
        RuleFor(command => command.ExpectedHarvestStart)
            .Must((command, value) => value is null || value >= command.StartDate);
        RuleFor(command => command.ExpectedHarvestEnd)
            .Must((command, value) => value is null || value >= command.ExpectedHarvestStart);
        RuleFor(command => command.ExpectedHarvestEnd)
            .Must((command, value) => value.HasValue == command.ExpectedHarvestStart.HasValue)
            .WithMessage("Provide both harvest window dates or leave both blank for calculated maturity.");
        RuleFor(command => command.ExpectedYieldTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000)
            .PrecisionScale(14, 3, true);
    }
}
