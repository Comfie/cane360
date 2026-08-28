using Cane360.Application.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollPreflightAssessmentTests
{
    public static IEnumerable<TestCaseData> EveryBlocker()
    {
        yield return Case(PayrollPreflightBlockerCodes.OutsidePayrollPeriod, input => input with { OutsidePayrollPeriod = true });
        yield return Case(PayrollPreflightBlockerCodes.AbsentAttendance, input => input with { AbsentAttendance = true });
        yield return Case(PayrollPreflightBlockerCodes.MissingFieldAllocation, input => input with { MissingFieldAllocation = true });
        yield return Case(PayrollPreflightBlockerCodes.ConflictingFieldAllocation, input => input with { ConflictingFieldAllocation = true });
        yield return Case(PayrollPreflightBlockerCodes.MissingSupervisorAttestation, input => input with { MissingSupervisorAttestation = true });
        yield return Case(PayrollPreflightBlockerCodes.MissingManagerConfirmation, input => input with { MissingManagerConfirmation = true });
        yield return Case(PayrollPreflightBlockerCodes.SupersededEvidence, input => input with { SupersededEvidence = true });
        yield return Case(PayrollPreflightBlockerCodes.InactiveEvidence, input => input with { InactiveEvidence = true });
        yield return Case(PayrollPreflightBlockerCodes.MissingRateSnapshot, input => input with { MissingRateSnapshot = true });
        yield return Case(PayrollPreflightBlockerCodes.MonthlyProrationUnresolved, input => input with { MonthlyProrationUnresolved = true });
        yield return Case(PayrollPreflightBlockerCodes.DuplicateOrScopeCollision, input => input with { DuplicateOrScopeCollision = true });
        yield return Case(PayrollPreflightBlockerCodes.CrossTenantOrFarmMismatch, input => input with { CrossTenantOrFarmMismatch = true });
        yield return Case(PayrollPreflightBlockerCodes.ArchivedWorker, input => input with { ArchivedWorker = true });
        yield return Case(PayrollPreflightBlockerCodes.SourceScopeMismatch, input => input with { SourceScopeMismatch = true });
    }

    [TestCaseSource(nameof(EveryBlocker))]
    public void EachReadinessDefectReturnsItsStableCode(
        string expectedCode,
        Func<PayrollPreflightAssessmentInput, PayrollPreflightAssessmentInput> arrange)
    {
        var result = PayrollPreflightAssessment.Assess(arrange(Eligible));

        result.Single().Code.ShouldBe(expectedCode);
        result.Single().Explanation.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void EligibleEvidenceHasNoBlockers()
    {
        PayrollPreflightAssessment.Assess(Eligible).ShouldBeEmpty();
    }

    [Test]
    public void SupersededEvidenceRemainsVisibleWithCorrectionBlocker()
    {
        var result = PayrollPreflightAssessment.Assess(Eligible with { SupersededEvidence = true });

        result.Single().Code.ShouldBe(PayrollPreflightBlockerCodes.SupersededEvidence);
        result.Single().Explanation.ShouldContain("append-only correction");
    }

    [Test]
    public void ArchivedWorkerRemainsHistoricallyVisibleButBlocked()
    {
        var result = PayrollPreflightAssessment.Assess(Eligible with { ArchivedWorker = true });

        result.Single().Code.ShouldBe(PayrollPreflightBlockerCodes.ArchivedWorker);
        result.Single().Explanation.ShouldContain("Historical evidence remains visible");
    }

    private static TestCaseData Case(
        string code,
        Func<PayrollPreflightAssessmentInput, PayrollPreflightAssessmentInput> arrange) =>
        new TestCaseData(code, arrange).SetName($"ReadinessDefectReturns_{code}");

    private static PayrollPreflightAssessmentInput Eligible => new(
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false);
}
