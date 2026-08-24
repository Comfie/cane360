using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class DecideOpeningBalanceCommandValidator : AbstractValidator<DecideOpeningBalanceCommand>
{
    public DecideOpeningBalanceCommandValidator()
    {
        RuleFor(command => command.Outcome).IsEnumName(typeof(ApprovalOutcome), false);
        RuleFor(command => command.Reason).MaximumLength(500);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(120);
    }
}
