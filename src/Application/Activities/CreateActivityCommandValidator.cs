using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropCycleId).NotEmpty();
        RuleFor(command => command.ActivityTypeId).NotEmpty();
        RuleFor(command => command.SupervisorPersonId).NotEmpty();
        RuleFor(command => command.Kind).IsEnumName(typeof(ActivityPlanningKind), false);
        RuleFor(command => command.PlannedDate).NotNull()
            .When(command => string.Equals(command.Kind, "Planned", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Planned work requires a planned date.");
    }
}
