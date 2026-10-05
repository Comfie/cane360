namespace Cane360.Application.Labour;

public sealed class VerifyWorkRecordCommandValidator : AbstractValidator<VerifyWorkRecordCommand>
{
    public VerifyWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkRecordId).NotEmpty();
        RuleFor(command => command.SupervisorPersonId).NotEmpty();
    }
}
