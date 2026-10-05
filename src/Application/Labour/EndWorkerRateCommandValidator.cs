namespace Cane360.Application.Labour;

public sealed class EndWorkerRateCommandValidator : AbstractValidator<EndWorkerRateCommand>
{
    public EndWorkerRateCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.RateId).NotEmpty();
        RuleFor(command => command.EffectiveTo).NotEmpty();
    }
}
