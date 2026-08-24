using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkRecordCommandValidator : AbstractValidator<CreateWorkRecordCommand>
{
    public CreateWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.WorkDate).NotEmpty();
        RuleFor(command => command.PayBasis).IsEnumName(typeof(PayBasis), false);
        RuleFor(command => command.ActivityIds).NotEmpty();
        RuleForEach(command => command.ActivityIds).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
        RuleFor(command => command.Scope!.Type).IsEnumName(typeof(WorkScopeType), false).When(command => command.Scope is not null);
        RuleFor(command => command.Scope!.SectionName).MaximumLength(120).When(command => command.Scope is not null);
    }
}
