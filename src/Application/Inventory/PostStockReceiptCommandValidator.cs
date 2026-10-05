namespace Cane360.Application.Inventory;

public sealed class PostStockReceiptCommandValidator : AbstractValidator<PostStockReceiptCommand>
{
    public PostStockReceiptCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(120);
    }
}
