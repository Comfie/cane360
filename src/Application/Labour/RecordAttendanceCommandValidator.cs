using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    public RecordAttendanceCommandValidator()
    {
        RuleFor(command => command.WorkDate).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
        RuleFor(command => command.Entries).NotEmpty();
        RuleForEach(command => command.Entries).ChildRules(entry =>
        {
            entry.RuleFor(item => item.WorkerId).NotEmpty();
            entry.RuleFor(item => item.Status).IsEnumName(typeof(AttendanceStatus), false);
            entry.RuleFor(item => item.FieldId).NotEmpty().When(item => string.Equals(item.Status, "Present", StringComparison.OrdinalIgnoreCase));
            entry.RuleFor(item => item.FieldId).Null().When(item => string.Equals(item.Status, "Absent", StringComparison.OrdinalIgnoreCase));
        });
    }
}
