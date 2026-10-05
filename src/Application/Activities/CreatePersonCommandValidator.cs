namespace Cane360.Application.Activities;

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.Roles).NotEmpty();
        RuleForEach(command => command.Roles).IsEnumName(typeof(PersonRole), false);
    }
}
