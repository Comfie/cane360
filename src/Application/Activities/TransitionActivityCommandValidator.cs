using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class TransitionActivityCommandValidator : AbstractValidator<TransitionActivityCommand>
{
    public TransitionActivityCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsEnumName(typeof(ActivityStatus), false);
        RuleFor(command => command.Reason).MaximumLength(500);
    }
}
