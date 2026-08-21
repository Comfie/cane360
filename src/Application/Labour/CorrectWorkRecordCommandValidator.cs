using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CorrectWorkRecordCommandValidator : AbstractValidator<CorrectWorkRecordCommand>
{
    public CorrectWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkRecordId).NotEmpty();
        RuleFor(command => command.CorrectionReason).NotEmpty().MaximumLength(500);
        RuleFor(command => command.PayBasis).IsEnumName(typeof(PayBasis), false);
        RuleFor(command => command.ActivityIds).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
    }
}
