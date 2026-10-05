namespace Cane360.Application.Labour;

public sealed class ArchiveWorkerCommandValidator : AbstractValidator<ArchiveWorkerCommand>
{
    public ArchiveWorkerCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.ActiveTo).NotEmpty();
    }
}
