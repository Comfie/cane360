using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateStockReceiptCommandValidator : AbstractValidator<CreateStockReceiptCommand>
{
    public CreateStockReceiptCommandValidator()
    {
        RuleFor(command => command.ReceiptType).IsEnumName(typeof(StockReceiptType), false);
        RuleFor(command => command.ReceiptDate).NotEmpty();
        RuleFor(command => command.SourceReference).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Reason).MaximumLength(500);
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
        RuleFor(command => command.Lines).NotEmpty();
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(value => value.InventoryItemId).NotEmpty();
            line.RuleFor(value => value.Quantity).GreaterThan(0);
            line.RuleFor(value => value.UnitCostUsd).GreaterThanOrEqualTo(0);
        });
    }
}
