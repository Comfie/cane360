namespace Cane360.Application.Labour;

public sealed class RevealWorkerNationalIdCommandValidator : AbstractValidator<RevealWorkerNationalIdCommand>
{
    public RevealWorkerNationalIdCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
