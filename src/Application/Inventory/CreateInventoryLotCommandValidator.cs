using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateInventoryLotCommandValidator : AbstractValidator<CreateInventoryLotCommand>
{
    public CreateInventoryLotCommandValidator()
    {
        RuleFor(command => command.InventoryItemId).NotEmpty();
        RuleFor(command => command.Code).NotEmpty().MaximumLength(60);
    }
}
