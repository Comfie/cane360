using Cane360.Domain.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollRunDomainTests
{
    [Test]
    public void LifecycleBindsExactImmutableCalculationVersion()
    {
        var run = Run();
        run.RecordCalculation(0).ShouldBe(1); run.RecordCalculation(1).ShouldBe(2); run.Submit(2, DateTimeOffset.UtcNow, "manager", 2);
        Should.Throw<InvalidOperationException>(() => run.Decide(true, 1, DateTimeOffset.UtcNow, null, 3));
        run.Decide(true, 2, DateTimeOffset.UtcNow, null, 3);
        run.Status.ShouldBe(PayrollRunStatus.Approved); run.Version.ShouldBe(4);
        Should.Throw<InvalidOperationException>(() => run.RecordCalculation(run.Version));
        Should.Throw<InvalidOperationException>(() => run.Cancel(DateTimeOffset.UtcNow, "rewrite", run.Version));
    }

    [Test]
    public void RejectedRunRequiresNewCalculationBeforeResubmission()
    {
        var run = Run(); run.RecordCalculation(0); run.Submit(1, DateTimeOffset.UtcNow, "manager", 1); run.Decide(false, 1, DateTimeOffset.UtcNow, "Incorrect allocation", 2);
        run.Status.ShouldBe(PayrollRunStatus.Rejected); run.RejectionReason.ShouldBe("Incorrect allocation");
        run.RecordCalculation(3).ShouldBe(2); run.Status.ShouldBe(PayrollRunStatus.Calculated); run.SubmittedCalculationVersion.ShouldBeNull();
    }

    [TestCase(1, 10.005, 10.01)]
    [TestCase(1.005, 10, 10.05)]
    [TestCase(2.555, 3.335, 8.52)]
    public void EarningLinesRoundAwayFromZeroOnlyAtFinalLine(decimal quantity, decimal rate, decimal expected)
    {
        var line = PayrollEarningLine.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "WorkRecord", new DateOnly(2026, 8, 1), Guid.NewGuid(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), "[]", quantity, "unit", "Hectare", rate, Guid.NewGuid(), 4, "fingerprint");
        line.Quantity.ShouldBe(quantity); line.RateAmountUsd.ShouldBe(rate); line.EarningAmountUsd.ShouldBe(expected);
    }

    [Test]
    public void WorkerAndRunTotalsAreExactSumsOfImmutableLines()
    {
        var calculationId = Guid.NewGuid(); var workerLineId = Guid.NewGuid(); var tenantId = Guid.NewGuid(); var farmId = Guid.NewGuid(); var workerId = Guid.NewGuid();
        var first = PayrollEarningLine.Create(workerLineId, calculationId, tenantId, farmId, workerId, Guid.NewGuid(), "WorkRecord", new DateOnly(2026, 8, 1), Guid.NewGuid(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), "[]", 1m, "day", "Daily", 10.005m, Guid.NewGuid(), 0, "one");
        var second = PayrollEarningLine.Create(workerLineId, calculationId, tenantId, farmId, workerId, Guid.NewGuid(), "WorkRecord", new DateOnly(2026, 8, 2), Guid.NewGuid(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), "[]", 2m, "hectare", "Hectare", 5.555m, Guid.NewGuid(), 0, "two");
        var deduction = PayrollAdvanceDeduction.Create(workerLineId, calculationId, tenantId, farmId, workerId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 20m, 8m, 8m);
        var worker = PayrollWorkerLine.Create(workerLineId, calculationId, tenantId, farmId, workerId, "Worker", [first, second], [deduction]);
        var calculation = PayrollCalculation.Create(calculationId, Guid.NewGuid(), Guid.NewGuid(), tenantId, farmId, 1, [worker], [], "run-fingerprint", DateTimeOffset.UtcNow, "manager", null);
        worker.GrossAmountUsd.ShouldBe(21.12m); worker.NetAmountUsd.ShouldBe(13.12m); calculation.GrossAmountUsd.ShouldBe(21.12m); calculation.DeductionAmountUsd.ShouldBe(8m); calculation.NetAmountUsd.ShouldBe(13.12m);
    }

    [Test]
    public void DeductionCannotExceedOutstandingOrProduceNegativeNet()
    {
        Should.Throw<InvalidOperationException>(() => PayrollAdvanceDeduction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 10m, 5m, 6m));
    }

    [Test]
    public void ApprovalClosesOpenPeriodWithExactRunMetadata()
    {
        var period = PayrollPeriod.Create(Guid.NewGuid(), Guid.NewGuid(), 2026, 8, DateTimeOffset.UtcNow, "manager", null); period.Open(DateTimeOffset.UtcNow, "manager", null, 0); var runId = Guid.NewGuid();
        period.Close(DateTimeOffset.UtcNow, "grower", null, runId, 1);
        period.Status.ShouldBe(PayrollPeriodStatus.Closed); period.ClosedByPayrollRunId.ShouldBe(runId); period.Version.ShouldBe(2);
        Should.Throw<InvalidOperationException>(() => period.Close(DateTimeOffset.UtcNow, "grower", null, runId, 2));
    }

    private static PayrollRun Run() => PayrollRun.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "manager", null);
}
