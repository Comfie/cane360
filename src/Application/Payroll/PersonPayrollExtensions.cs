namespace Cane360.Application.Payroll;

internal static class PersonPayrollExtensions
{
    public static string FullName(this Person person)
    {
        return person.DisplayName;
    }
}
