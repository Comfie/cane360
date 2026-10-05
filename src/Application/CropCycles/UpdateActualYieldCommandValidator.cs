namespace Cane360.Application.CropCycles;

public sealed class UpdateActualYieldCommandValidator : AbstractValidator<UpdateActualYieldCommand>
{
    public UpdateActualYieldCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropCycleId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ActualTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000)
            .PrecisionScale(14, 3, true);
    }
}
