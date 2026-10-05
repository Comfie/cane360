namespace Cane360.Application.Activities;

public sealed class AddSourceReferenceCommandValidator : AbstractValidator<AddSourceReferenceCommand>
{
    public AddSourceReferenceCommandValidator()
    {
        RuleFor(command => command.SourceSheetReference).NotEmpty().MaximumLength(160);
        RuleFor(command => command.CapturedDate).NotEmpty();
    }
}
