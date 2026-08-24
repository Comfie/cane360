using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CancelCropCycleCommandValidator : AbstractValidator<CancelCropCycleCommand>
{
    public CancelCropCycleCommandValidator() =>
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
}
