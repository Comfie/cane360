namespace Cane360.Application.Inventory;

public sealed class SetInventoryCategoryActiveCommandValidator : AbstractValidator<SetInventoryCategoryActiveCommand>
{
    public SetInventoryCategoryActiveCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).GreaterThan(0);
    }
}
