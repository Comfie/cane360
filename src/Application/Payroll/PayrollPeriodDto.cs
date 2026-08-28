namespace Cane360.Application.Payroll;

public sealed record PayrollPeriodDto(Guid Id, int Year, int Month, DateOnly StartDate, DateOnly EndDate, string DisplayName, string Status, long Version);
