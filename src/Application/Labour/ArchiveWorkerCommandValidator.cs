using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class ArchiveWorkerCommandValidator : AbstractValidator<ArchiveWorkerCommand>
{
    public ArchiveWorkerCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.ActiveTo).NotEmpty();
    }
}
