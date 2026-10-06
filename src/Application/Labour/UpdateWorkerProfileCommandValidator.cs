using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class UpdateWorkerProfileCommandValidator : AbstractValidator<UpdateWorkerProfileCommand>
{
    public UpdateWorkerProfileCommandValidator(TimeProvider clock)
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ExpectedPersonVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.EmploymentType).IsEnumName(typeof(EmploymentType), false);
        RuleFor(command => command.Profile).NotNull().SetValidator(new WorkerProfileInputValidator(clock));
    }
}
