namespace Cane360.Application.Labour;

public sealed class CorrectWorkerNationalIdCommandValidator : AbstractValidator<CorrectWorkerNationalIdCommand>
{
    public CorrectWorkerNationalIdCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.NationalId).NotEmpty().MaximumLength(80);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
