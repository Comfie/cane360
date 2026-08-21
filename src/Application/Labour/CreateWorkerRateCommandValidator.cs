using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkerRateCommandValidator : AbstractValidator<CreateWorkerRateCommand>
{
    public CreateWorkerRateCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.Basis).IsEnumName(typeof(PayBasis), false);
        RuleFor(command => command.RateUsd).GreaterThan(0);
        RuleFor(command => command.EffectiveFrom).NotEmpty();
        RuleFor(command => command.EffectiveTo).GreaterThanOrEqualTo(command => command.EffectiveFrom)
            .When(command => command.EffectiveTo.HasValue);
    }
}
