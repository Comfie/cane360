using Cane360.Domain.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollFoundationDomainTests
{
    [Test]
    public void PayrollPeriodUsesExactCalendarMonthBoundaries()
    {
        var period = PayrollPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 2028, 2, DateTimeOffset.UtcNow, "user", null);
        period.StartDate.ShouldBe(new DateOnly(2028, 2, 1));
        period.EndDate.ShouldBe(new DateOnly(2028, 2, 29));
    }

    [Test]
    public void AdvanceDefaultSchedulePlacesRoundingResidualInFinalInstallment()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "Transport", new DateOnly(2026, 8, 26), Guid.NewGuid(), 3, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()], 0);
        advance.Installments.Select(x => x.AmountUsd).ShouldBe([33.33m, 33.33m, 33.34m]);
        advance.Installments.Sum(x => x.AmountUsd).ShouldBe(100m);
    }

    [Test]
    public void StaleAdvanceVersionIsRejected()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30m, "Transport", new DateOnly(2026, 8, 26), Guid.NewGuid(), 1, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([Guid.NewGuid()], 0);
        Should.Throw<InvalidOperationException>(() => advance.Submit(0));
    }

    [Test]
    public void CashIssueRequiresWorkerAcknowledgement()
    {
        Should.Throw<InvalidOperationException>(() => AdvanceIssue.Cash(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, DateTimeOffset.UtcNow, "manager", Guid.NewGuid(), Guid.NewGuid(), false, "cash-1"));
    }

    [Test]
    public void PayrollPeriodRejectsStaleOpenAndCancelVersions()
    {
        var period = PayrollPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 2028, 3, DateTimeOffset.UtcNow, "user", null);
        Should.Throw<InvalidOperationException>(() => period.Open(DateTimeOffset.UtcNow, "user", null, 1));
        Should.Throw<InvalidOperationException>(() => period.Cancel(DateTimeOffset.UtcNow, "user", null, "No longer needed", 1));
    }

    [Test]
    public void OpenPayrollPeriodCannotBeCancelledInPhase6A()
    {
        var period = PayrollPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 2028, 3, DateTimeOffset.UtcNow, "user", null); period.Open(DateTimeOffset.UtcNow, "user", null, 0);
        Should.Throw<InvalidOperationException>(() => period.Cancel(DateTimeOffset.UtcNow, "user", null, "No longer needed", period.Version));
    }

    [TestCase(1, 100, 100)]
    [TestCase(2, 50, 50)]
    [TestCase(4, 25, 25)]
    public void ConfigurableInstallmentCountCreatesExactSchedule(int count, decimal first, decimal last)
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, "School", new DateOnly(2026, 8, 27), Guid.NewGuid(), count, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule(Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray(), 0);
        advance.Installments.Count.ShouldBe(count); advance.Installments.First().AmountUsd.ShouldBe(first); advance.Installments.Last().AmountUsd.ShouldBe(last); advance.Installments.Sum(item => item.AmountUsd).ShouldBe(100m);
    }

    [Test]
    public void RejectedAdvanceCanBeMateriallyRevisedAndRequiresResubmission()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 90m, "Transport", new DateOnly(2026, 8, 27), Guid.NewGuid(), 3, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()], 0); advance.Submit(1); advance.Decide(false, 2);
        advance.Edit(100m, "School", new DateOnly(2026, 8, 28), Guid.NewGuid(), 2, 3); advance.SetSchedule([Guid.NewGuid(), Guid.NewGuid()], 4);
        advance.Status.ShouldBe(AdvanceStatus.Draft); advance.ApprovedAmountUsd.ShouldBeNull(); advance.Installments.Sum(item => item.AmountUsd).ShouldBe(100m);
    }

    [Test]
    public void ApprovalDoesNotIssueAdvanceAutomatically()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30m, "Transport", new DateOnly(2026, 8, 27), Guid.NewGuid(), 1, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([Guid.NewGuid()], 0); advance.Submit(1); advance.Decide(true, 2);
        advance.Status.ShouldBe(AdvanceStatus.Approved); advance.ApprovedAmountUsd.ShouldBe(30m);
    }

    [Test]
    public void IssuedAmountMustEqualExactApprovedAmount()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30m, "Transport", new DateOnly(2026, 8, 27), Guid.NewGuid(), 1, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([Guid.NewGuid()], 0); advance.Submit(1); advance.Decide(true, 2);
        Should.Throw<InvalidOperationException>(() => advance.Issue(29.99m, 3));
    }

    [TestCase(null, "•••• 0123", "REF", "Confirmed")]
    [TestCase("EcoCash", null, "REF", "Confirmed")]
    [TestCase("EcoCash", "•••• 0123", null, "Confirmed")]
    [TestCase("EcoCash", "•••• 0123", "REF", null)]
    public void MobileMoneyRequiresEveryEvidenceField(string? provider, string? recipient, string? reference, string? status)
    {
        Should.Throw<InvalidOperationException>(() => AdvanceIssue.MobileMoney(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, DateTimeOffset.UtcNow, "manager", provider!, recipient!, reference!, status!, "mobile-key"));
    }

    [Test]
    public void IssueEvidenceRequiresPositiveAmountAndTimestamp()
    {
        Should.Throw<InvalidOperationException>(() => AdvanceIssue.Cash(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, DateTimeOffset.UtcNow, "manager", Guid.NewGuid(), Guid.NewGuid(), true, "cash-key"));
        Should.Throw<InvalidOperationException>(() => AdvanceIssue.Cash(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, default, "manager", Guid.NewGuid(), Guid.NewGuid(), true, "cash-key"));
    }
}
