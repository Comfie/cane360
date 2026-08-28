using Cane360.Domain.Activities;

namespace Cane360.Application.Payroll;

internal static class PersonPayrollExtensions
{
    public static string FullName(this Person person) => person.DisplayName;
}
