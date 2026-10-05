namespace Cane360.Application.Labour;

public sealed class ConfirmWorkRecordCommandValidator : AbstractValidator<ConfirmWorkRecordCommand>
{
    public ConfirmWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkRecordId).NotEmpty();
    }
}
