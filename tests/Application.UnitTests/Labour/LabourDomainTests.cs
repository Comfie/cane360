using Cane360.Domain.Labour;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public class LabourDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid WorkerId = Guid.NewGuid();
    private static readonly DateOnly WorkDate = new(2026, 8, 18);
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void AttendanceRequiresExactlyOneFieldWhenPresent()
    {
        Should.Throw<InvalidOperationException>(() => Attendance.Create(
            TenantId, FarmId, WorkerId, WorkDate, AttendanceStatus.Present, null,
            Now, "manager-1", null, 0)).Message.ShouldContain("exactly one field");

        Should.Throw<InvalidOperationException>(() => Attendance.Create(
            TenantId, FarmId, WorkerId, WorkDate, AttendanceStatus.Absent, Guid.NewGuid(),
            Now, "manager-1", null, 0)).Message.ShouldContain("cannot have");
    }

    [Test]
    public void OverlappingRateUsesWorkerBasisAndActivityScope()
    {
        var activityTypeId = Guid.NewGuid();
        var first = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.Hectare,
            activityTypeId, 20m, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        var overlapping = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.Hectare,
            activityTypeId, 22m, new DateOnly(2026, 8, 31), null);
        var otherScope = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.Hectare,
            Guid.NewGuid(), 22m, new DateOnly(2026, 8, 31), null);

        first.Overlaps(overlapping).ShouldBeTrue();
        first.Overlaps(otherScope).ShouldBeFalse();
    }

    [Test]
    public void PieceEvidenceSnapshotsRateAndRequiresBothVerificationStages()
    {
        var rate = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.StandardLine,
            Guid.NewGuid(), 1.25m, new DateOnly(2026, 8, 1), null);
        var record = WorkRecord.Create(TenantId, FarmId, Guid.NewGuid(), WorkerId,
            Guid.NewGuid(), WorkDate, rate, 10m, [Guid.NewGuid()], Now, "manager-1", null, 0);

        record.AppliedRateUsd.ShouldBe(1.25m);
        Should.Throw<InvalidOperationException>(() => record.Confirm(Now, "manager-1", 0));

        record.RecordSupervisorVerification(Guid.NewGuid(), Now, "manager-1", 0);
        record.Status.ShouldBe(WorkRecordStatus.SupervisorVerified);
        record.Confirm(Now.AddMinutes(1), "manager-1", 1);

        record.Status.ShouldBe(WorkRecordStatus.Confirmed);
        record.CalculatedAmountUsd.ShouldBe(12.50m);
    }

    [Test]
    public void MonthlyConfirmationDefersAmountToFuturePayroll()
    {
        var rate = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.Monthly,
            null, 450m, new DateOnly(2026, 8, 1), null);
        var record = WorkRecord.Create(TenantId, FarmId, Guid.NewGuid(), WorkerId,
            Guid.NewGuid(), WorkDate, rate, null, [Guid.NewGuid()], Now, "manager-1", null, 0);

        record.RecordSupervisorVerification(Guid.NewGuid(), Now, "manager-1", 0);
        record.Confirm(Now, "manager-1", 1);

        record.CalculatedAmountUsd.ShouldBeNull();
        record.AppliedRateUsd.ShouldBe(450m);
    }

    [Test]
    public void ConfirmedEvidenceCanOnlyBeChangedByExplicitSupersession()
    {
        var rate = WorkerRate.Create(TenantId, FarmId, WorkerId, PayBasis.Daily,
            null, 12m, new DateOnly(2026, 8, 1), null);
        var record = WorkRecord.Create(TenantId, FarmId, Guid.NewGuid(), WorkerId,
            Guid.NewGuid(), WorkDate, rate, null, [Guid.NewGuid()], Now, "manager-1", null, 0);
        record.RecordSupervisorVerification(Guid.NewGuid(), Now, "manager-1", 0);
        record.Confirm(Now, "manager-1", 1);

        Should.Throw<InvalidOperationException>(() => record.AddNamedSection(Guid.NewGuid(), "North"));
        record.Supersede("Correct activity allocation", "manager-1", Now.AddMinutes(2), 2);
        record.Status.ShouldBe(WorkRecordStatus.Superseded);
        record.CorrectionReason.ShouldBe("Correct activity allocation");
    }
}
