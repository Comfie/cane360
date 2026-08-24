using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed class CreateCropVarietyCommandValidator : AbstractValidator<CreateCropVarietyCommand>
{
    public CreateCropVarietyCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(80);
    }
}
