using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class ReverseStockReceiptCommandValidator : AbstractValidator<ReverseStockReceiptCommand>
{
    public ReverseStockReceiptCommandValidator()
    {
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(120);
    }
}
