using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateUnitOfMeasureCommandValidator : AbstractValidator<CreateUnitOfMeasureCommand>
{
    public CreateUnitOfMeasureCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Dimension).NotEmpty().MaximumLength(40);
        RuleFor(command => command.DecimalPlaces).InclusiveBetween(0, 6);
    }
}
