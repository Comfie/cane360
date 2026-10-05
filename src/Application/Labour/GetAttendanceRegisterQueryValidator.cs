namespace Cane360.Application.Labour;

public sealed class GetAttendanceRegisterQueryValidator : AbstractValidator<GetAttendanceRegisterQuery>
{
    public GetAttendanceRegisterQueryValidator()
    {
        RuleFor(query => query.WorkDate).NotEmpty();
    }
}
