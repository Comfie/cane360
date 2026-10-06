using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class WorkerProfileInputValidator : AbstractValidator<WorkerProfileInput>
{
    public WorkerProfileInputValidator(TimeProvider clock)
    {
        RuleFor(input => input.EmployeeNumber).MaximumLength(60);
        RuleFor(input => input.Title).MaximumLength(40);
        RuleFor(input => input.FirstName).MaximumLength(60);
        RuleFor(input => input.Surname).MaximumLength(59);
        RuleFor(input => input.Sex).MaximumLength(40);
        RuleFor(input => input.Address).MaximumLength(240);
        RuleFor(input => input.PhotoReference).MaximumLength(240);
        RuleFor(input => input.NextOfKinName).MaximumLength(120);
        RuleFor(input => input.NextOfKinRelationship).MaximumLength(60);
        RuleFor(input => input.NextOfKinPhone).MaximumLength(30);
        RuleFor(input => input.NextOfKinAddress).MaximumLength(240);
        RuleFor(input => input.Sex).Must(value => string.IsNullOrWhiteSpace(value) ||
            new[] { "Female", "Male", "Other", "Prefer not to say" }.Contains(value));
        RuleFor(input => input.Surname).Must((input, value) =>
            string.IsNullOrWhiteSpace(input.FirstName) == string.IsNullOrWhiteSpace(value))
            .WithMessage("Supply both first name and surname.");
        RuleFor(input => input.DateOfBirth).Must(value => value is null ||
            value <= LabourAccess.HarareDate(clock.GetUtcNow())).WithMessage("Date of birth cannot be in the future.");
    }
}
