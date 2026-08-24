using Cane360.Domain.Activities;

namespace Cane360.Application.Activities;

public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.Role).IsEnumName(typeof(PersonRole), false);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
    }
}
