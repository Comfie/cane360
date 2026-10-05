namespace Cane360.Application.FarmSetup;

public sealed class UpdateFieldDetailsCommandValidator : AbstractValidator<UpdateFieldDetailsCommand>
{
    public UpdateFieldDetailsCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.IrrigationMethod).NotEmpty().MaximumLength(100);
        RuleFor(command => command.SoilNotes).MaximumLength(500);
    }
}
