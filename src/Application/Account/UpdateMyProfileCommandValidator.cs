namespace Cane360.Application.Account;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.PhoneNumber).MaximumLength(40)
            .Matches(@"^[0-9+()\-\s]*$").WithMessage("Contact number must contain only digits, spaces, +, -, or parentheses.");
    }
}
