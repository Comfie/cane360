using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class ReplaceFieldLineProfileCommandValidator : AbstractValidator<ReplaceFieldLineProfileCommand>
{
    public ReplaceFieldLineProfileCommandValidator()
    {
        RuleFor(command => command.StandardLineLengthMetres).GreaterThan(0);
        RuleFor(command => command.EstimatedLineCount).GreaterThan(0);
        RuleFor(command => command.NumberingScheme).NotEmpty().MaximumLength(240);
    }
}
