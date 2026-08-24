using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class CreateActivityTypeCommandValidator : AbstractValidator<CreateActivityTypeCommand>
{
    public CreateActivityTypeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(24).Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command).Must(command => command.SupportsPlanned || command.SupportsUnplanned)
            .WithMessage("At least one planning mode is required.");
        RuleFor(command => command.QuantityBasis).IsEnumName(typeof(ActivityQuantityBasis), false);
    }
}
