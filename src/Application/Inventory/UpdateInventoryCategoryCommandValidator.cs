namespace Cane360.Application.Inventory;

public sealed class UpdateInventoryCategoryCommandValidator : AbstractValidator<UpdateInventoryCategoryCommand>
{
    public UpdateInventoryCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.DisplayOrder).InclusiveBetween(0, 10000);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.ExpectedVersion).GreaterThan(0);
    }
}
