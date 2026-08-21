using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkerCommandValidator : AbstractValidator<CreateWorkerCommand>
{
    public CreateWorkerCommandValidator()
    {
        RuleFor(command => command.PersonId).NotEmpty().When(command => command.PersonId.HasValue);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120).When(command => !command.PersonId.HasValue);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.EmploymentType).IsEnumName(typeof(EmploymentType), false);
        RuleFor(command => command.ActiveFrom).NotEmpty();
        RuleFor(command => command.NationalId).NotEmpty().MaximumLength(80);
    }
}
