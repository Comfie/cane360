namespace Cane360.Application.Inventory;

public sealed class CreateInventoryLotCommandValidator : AbstractValidator<CreateInventoryLotCommand>
{
    public CreateInventoryLotCommandValidator()
    {
        RuleFor(command => command.InventoryItemId).NotEmpty();
        RuleFor(command => command.Code).NotEmpty().MaximumLength(60);
    }
}
