namespace Cane360.Application.Activities;

public sealed class TransitionActivityCommandValidator : AbstractValidator<TransitionActivityCommand>
{
    public TransitionActivityCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsEnumName(typeof(ActivityStatus), false);
        RuleFor(command => command.Reason).MaximumLength(500);
    }
}
