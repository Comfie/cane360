using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class RecordActualWorkCommandValidator : AbstractValidator<RecordActualWorkCommand>
{
    public RecordActualWorkCommandValidator()
    {
        RuleFor(command => command.ActivityId).NotEmpty();
        RuleFor(command => command.ActualAt).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
    }
}
