namespace Cane360.Application.CropCycles;

public sealed class CalculateCropMaturityQueryValidator : AbstractValidator<CalculateCropMaturityQuery>
{
    public CalculateCropMaturityQueryValidator()
    {
        RuleFor(query => query.FieldId).NotEmpty();
        RuleFor(query => query.PlantingDate).Must(date => date is null || date.Value.Year <= 9989);
    }
}
