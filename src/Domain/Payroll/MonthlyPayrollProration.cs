namespace Cane360.Domain.Payroll;

public static class MonthlyPayrollProration
{
    public static decimal EarningForDay(decimal monthlyRateUsd, int daysInMonth, int verifiedDayNumber)
    {
        if (monthlyRateUsd <= 0 || daysInMonth is < 28 or > 31 ||
            verifiedDayNumber < 1 || verifiedDayNumber > daysInMonth)
            throw new ArgumentOutOfRangeException(nameof(verifiedDayNumber));

        decimal throughDay = decimal.Round(monthlyRateUsd * verifiedDayNumber / daysInMonth,
            2, MidpointRounding.AwayFromZero);
        decimal throughPreviousDay = decimal.Round(monthlyRateUsd * (verifiedDayNumber - 1) / daysInMonth,
            2, MidpointRounding.AwayFromZero);
        return throughDay - throughPreviousDay;
    }
}
