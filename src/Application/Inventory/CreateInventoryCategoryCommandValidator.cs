namespace Cane360.Application.Inventory;

public sealed class CreateInventoryCategoryCommandValidator : AbstractValidator<CreateInventoryCategoryCommand>
{
    public CreateInventoryCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.DisplayOrder).InclusiveBetween(0, 10000);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(40).Matches("^[A-Za-z0-9_-]+$");
    }
}
