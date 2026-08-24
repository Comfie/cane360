using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class HarvestCropCycleCommandValidator : AbstractValidator<HarvestCropCycleCommand>
{
    public HarvestCropCycleCommandValidator()
    {
        RuleFor(command => command.HarvestDate).NotEmpty();
        RuleFor(command => command.ActualTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}
