namespace Cane360.Application.FarmSetup;

public sealed class SaveFarmModelCommandValidator : AbstractValidator<SaveFarmModelCommand>
{
    public SaveFarmModelCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(24).Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.ExpectedVersion).NotNull().GreaterThan(0).When(command => command.Id is not null);
        RuleFor(command => command.Active).Equal(true).When(command => command.Id is null);
    }
}
