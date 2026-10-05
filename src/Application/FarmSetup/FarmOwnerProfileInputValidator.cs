namespace Cane360.Application.FarmSetup;

public sealed class FarmOwnerProfileInputValidator : AbstractValidator<FarmOwnerProfileInput>
{
    public FarmOwnerProfileInputValidator()
    {
        RuleFor(input => input.Title).MaximumLength(40);
        RuleFor(input => input.FirstName).MaximumLength(100);
        RuleFor(input => input.Surname).MaximumLength(100);
        RuleFor(input => input.Sex).MaximumLength(40);
        RuleFor(input => input.GrowerNumber).MaximumLength(60);
        RuleFor(input => input.Association).MaximumLength(120);
        RuleFor(input => input.MembershipNumber).MaximumLength(60);
        RuleFor(input => input.RegisteredAddress).MaximumLength(240);
        RuleFor(input => input.Email).MaximumLength(254);
        RuleFor(input => input.PhotoReference).MaximumLength(240);
        RuleFor(input => input.Email).EmailAddress().When(input => !string.IsNullOrWhiteSpace(input.Email));
        RuleFor(input => input.Sex).Must(value => string.IsNullOrWhiteSpace(value) ||
            new[] { "Female", "Male", "Other", "Prefer not to say" }.Contains(value));
        RuleFor(input => input.NationalId).MaximumLength(80).Must(value => string.IsNullOrWhiteSpace(value) ||
            value.Count(char.IsLetterOrDigit) is >= 4 and <= 40);
    }
}
