using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetAttendanceRegisterQueryValidator : AbstractValidator<GetAttendanceRegisterQuery>
{
    public GetAttendanceRegisterQueryValidator() => RuleFor(query => query.WorkDate).NotEmpty();
}
