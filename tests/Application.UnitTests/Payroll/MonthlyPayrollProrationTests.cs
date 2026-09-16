using Cane360.Domain.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class MonthlyPayrollProrationTests
{
    [TestCase(28)]
    [TestCase(29)]
    [TestCase(30)]
    [TestCase(31)]
    public void EveryVerifiedCalendarDayTotalsTheExactMonthlyRate(int daysInMonth)
    {
        decimal total = Enumerable.Range(1, daysInMonth)
            .Sum(day => MonthlyPayrollProration.EarningForDay(55m, daysInMonth, day));

        total.ShouldBe(55m);
    }

    [Test]
    public void PartialMonthUsesVerifiedDaysAndRoundsAtCumulativeBoundaries()
    {
        MonthlyPayrollProration.EarningForDay(400m, 31, 1).ShouldBe(12.90m);
        MonthlyPayrollProration.EarningForDay(400m, 31, 2).ShouldBe(12.91m);
    }
}
