using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(30).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Category).IsEnumName(typeof(InventoryItemCategory), false);
        RuleFor(command => command.StockUnitId).NotEmpty();
        RuleFor(command => command.ReorderLevel).GreaterThanOrEqualTo(0).When(command => command.ReorderLevel.HasValue);
        RuleFor(command => command.LotTrackingPolicy).IsEnumName(typeof(LotTrackingPolicy), false);
        RuleFor(command => command.ExpiryPolicy).IsEnumName(typeof(ExpiryPolicy), false);
    }
}
