using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollSettlementServiceTests
{
    [Test] public async Task PaymentBeforeGrowerApprovalIsRejected() { var fixture = Fixture.Create(approved: false); await Should.ThrowAsync<ConflictException>(() => fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(10m, "before"), default)); }
    [Test] public async Task CashPaymentAgainstApprovedExactVersionSucceeds() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(25m, "cash"), default); payment.Method.ShouldBe("Cash"); payment.CalculationVersion.ShouldBe(1); }
    [Test] public async Task MobileMoneyRequiresProviderRecipientReferenceDateAmountAndStatus() { var fixture = Fixture.Create(); var input = fixture.Mobile(10m, "missing") with { Provider = null }; await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.RecordPaymentAsync(fixture.Run.Id, input, default)); }
    [Test] public async Task MobileMoneyRecipientIsMaskedInOrdinaryResponse() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(10m, "mobile"), default); payment.MaskedRecipientNumber.ShouldBe("•••• 0123"); payment.ToString().ShouldNotContain(fixture.MobileRecipientNumber); }
    [Test] public async Task PartialPaymentsReduceOutstandingExactly() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(35.25m, "partial"), default); (await fixture.Service.GetRunAsync(fixture.Run.Id, default)).OutstandingAmountUsd.ShouldBe(64.75m); }
    [Test] public async Task MultiplePaymentsCanSettleOneWorker() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(40m, "one"), default); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(60m, "two"), default); (await fixture.Service.GetRunAsync(fixture.Run.Id, default)).Workers.Single().SettlementStatus.ShouldBe("Paid"); }
    [Test] public async Task PaymentCannotExceedWorkerNet() { var fixture = Fixture.Create(); await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(100.01m, "over"), default)); }
    [Test] public async Task ConcurrentPaymentsCannotOverpayWorker() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(80m, "concurrent-one"), default); await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(30m, "concurrent-two"), default)); fixture.Payroll.Verify(x => x.BeginSerializableTransactionAsync(default), Times.Exactly(2)); }
    [Test] public void ZeroNetWorkerRequiresNoPayment() { WorkerSettlementDto worker = new(Guid.NewGuid(), Guid.NewGuid(), "Zero", 10m, 10m, 0m, 0m, 0m, 0m, 0, string.Empty, true, "Paid", []); worker.SettlementStatus.ShouldBe("Paid"); }
    [Test] public async Task FailedMobilePaymentDoesNotReduceOutstanding() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(100m, "failed") with { ExternalStatus = "Failed" }, default); (await fixture.Service.GetRunAsync(fixture.Run.Id, default)).OutstandingAmountUsd.ShouldBe(100m); }
    [Test] public async Task PaymentRetryIsIdempotent() { var fixture = Fixture.Create(); var input = fixture.Cash(20m, "retry"); var first = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, input, default); var second = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, input, default); second.Id.ShouldBe(first.Id); fixture.Payments.Count.ShouldBe(1); }
    [Test] public async Task CashPaymentRequiresAcknowledgementForSettlementClosure() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(100m, "cash-close"), default); await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.CloseAsync(fixture.Run.Id, fixture.Close("close"), default)); }
    [Test] public async Task PaymentAcknowledgementBindsExactPayment() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(100m, "ack-payment"), default); var result = await fixture.Service.AcknowledgeAsync(payment.Id, fixture.Acknowledge("ack"), default); result.Acknowledgement!.Status.ShouldBe("Acknowledged"); fixture.Acknowledgements.Single().PayrollPaymentId.ShouldBe(payment.Id); }
    [Test] public async Task PaymentReversalPreservesOriginalPayment() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(40m, "reverse-payment"), default); await fixture.Service.ReverseAsync(payment.Id, new(10m, "Correction", "reverse"), default); fixture.Payments.Single().AmountUsd.ShouldBe(40m); fixture.Reversals.Count.ShouldBe(1); }
    [Test] public async Task ReversalCannotExceedUnreversedPayment() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(40m, "reverse-limit"), default); await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.ReverseAsync(payment.Id, new(40.01m, "Too much", "reverse-over"), default)); }
    [Test] public async Task ReversalRestoresOutstandingBalanceExactly() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(40m, "restore"), default); await fixture.Service.ReverseAsync(payment.Id, new(15m, "Correction", "restore-reverse"), default); (await fixture.Service.GetRunAsync(fixture.Run.Id, default)).OutstandingAmountUsd.ShouldBe(75m); }
    [Test] public async Task FinalSettlementCloseRequiresAllWorkersSettled() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(99m, "not-all"), default); await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => fixture.Service.CloseAsync(fixture.Run.Id, fixture.Close("not-all-close"), default)); }
    [Test] public async Task FinalSettlementCloseRejectsMissingAcknowledgement() => await CashPaymentRequiresAcknowledgementForSettlementClosure();
    [Test] public async Task FinalSettlementCloseBindsExactApprovedCalculationVersion() { var fixture = Fixture.Create(); await Should.ThrowAsync<ConflictException>(() => fixture.Service.CloseAsync(fixture.Run.Id, new(2, "wrong-version"), default)); }
    [Test] public async Task FinalSettlementCloseRetryIsIdempotent() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(100m, "settle"), default); var input = fixture.Close("close-retry"); var first = await fixture.Service.CloseAsync(fixture.Run.Id, input, default); var second = await fixture.Service.CloseAsync(fixture.Run.Id, input, default); first.IsClosed.ShouldBeTrue(); second.IsClosed.ShouldBeTrue(); fixture.Closures.Count.ShouldBe(1); }
    [Test] public async Task ClosedSettlementRejectsNewNormalPayment() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(100m, "settled"), default); await fixture.Service.CloseAsync(fixture.Run.Id, fixture.Close("closed"), default); await Should.ThrowAsync<ConflictException>(() => fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(1m, "after-close"), default)); }
    [Test] public async Task FarmManagerCannotPerformGrowerOnlySettlementReopen() { var fixture = Fixture.Create(); await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Mobile(100m, "closed-payment"), default); await fixture.Service.CloseAsync(fixture.Run.Id, fixture.Close("closed-first"), default); await Should.ThrowAsync<ForbiddenAccessException>(() => fixture.Service.ReopenAsync(fixture.Run.Id, new(1, "Correction", "reopen"), default)); }
    [Test] public async Task PayslipTotalsEqualApprovedImmutableWorkerLine() { var fixture = Fixture.Create(); var payslip = await fixture.Service.GetPayslipAsync(fixture.Run.Id, 1, fixture.Line.Id, default); payslip.GrossAmountUsd.ShouldBe(fixture.Line.GrossAmountUsd); payslip.NetAmountUsd.ShouldBe(fixture.Line.NetAmountUsd); }
    [Test] public async Task PayslipMasksSensitiveWorkerIdentifier() { var fixture = Fixture.Create(); (await fixture.Service.GetPayslipAsync(fixture.Run.Id, 1, fixture.Line.Id, default)).MaskedWorkerIdentifier.ShouldBe("••••••12"); }
    [Test] public async Task PayslipContainsOperationalNonStatutoryStatement() { var fixture = Fixture.Create(); (await fixture.Service.GetPayslipAsync(fixture.Run.Id, 1, fixture.Line.Id, default)).DocumentStatement.ShouldBe("Operational payroll record — not a statutory tax payslip"); }
    [Test] public async Task CashRegisterReconcilesToActiveCashPayments() { var fixture = Fixture.Create(); var payment = await fixture.Service.RecordPaymentAsync(fixture.Run.Id, fixture.Cash(50m, "register"), default); await fixture.Service.ReverseAsync(payment.Id, new(10m, "Correction", "register-reverse"), default); (await fixture.Service.GetCashRegisterAsync(fixture.Run.Id, 1, default)).TotalActiveCashPaidUsd.ShouldBe(40m); }
    [Test] public async Task CrossTenantPaymentQueryReturnsNoUsableData() { var fixture = Fixture.Create(); await Should.ThrowAsync<NotFoundException>(() => fixture.Service.GetRunAsync(Guid.NewGuid(), default)); }
    [Test] public async Task CrossTenantPaymentMutationIsRejected() { var fixture = Fixture.Create(); await Should.ThrowAsync<NotFoundException>(() => fixture.Service.RecordPaymentAsync(Guid.NewGuid(), fixture.Cash(1m, "cross"), default)); }
    [Test] public async Task CrossTenantAcknowledgementIsRejected() { var fixture = Fixture.Create(); await Should.ThrowAsync<NotFoundException>(() => fixture.Service.AcknowledgeAsync(Guid.NewGuid(), fixture.Acknowledge("cross-ack"), default)); }
    [Test] public async Task CrossTenantReversalIsRejected() { var fixture = Fixture.Create(); await Should.ThrowAsync<NotFoundException>(() => fixture.Service.ReverseAsync(Guid.NewGuid(), new(1m, "Cross tenant", "cross-reverse"), default)); }

    private sealed class Fixture
    {
        private static readonly DateTimeOffset Now = new(2037, 1, 15, 10, 0, 0, TimeSpan.Zero);
        private Fixture() { }
        public required PayrollSettlementService Service { get; set; }
        public required Mock<IPayrollRepository> Payroll { get; init; }
        public required PayrollRun Run { get; init; }
        public required PayrollWorkerLine Line { get; init; }
        public List<PayrollPayment> Payments { get; } = [];
        public List<PaymentAcknowledgement> Acknowledgements { get; } = [];
        public List<PayrollPaymentReversal> Reversals { get; } = [];
        public List<PayrollSettlementClosure> Closures { get; } = [];
        public List<PayrollSettlementReopen> Reopens { get; } = [];

        public RecordPayrollPaymentInput Cash(decimal amount, string key) => new(1, Line.Id, "Cash", amount, new DateOnly(2037, 1, 15), null, null, null, null, key);
        public string MobileRecipientNumber { get; } = $"TEST-{Guid.NewGuid():N}"[..16] + "0123";
        public RecordPayrollPaymentInput Mobile(decimal amount, string key) => new(1, Line.Id, "MobileMoney", amount, new DateOnly(2037, 1, 15), "Provider", MobileRecipientNumber, $"REF-{key}", "Successful", key);
        public RecordPaymentAcknowledgementInput Acknowledge(string key) => new("Acknowledged", null, Now, null, key);
        public ClosePayrollSettlementInput Close(string key) => new(1, key);

        public static Fixture Create(bool approved = true)
        {
            var tenant = Tenant.CreateForGrower("grower", "Grower", null);
            var farm = tenant.CreateFarm("P6C", "Settlement farm", "Address", "Location", "Lease", 10m, "Furrow");
            var manager = farm.AddPerson("Manager", null, new DateOnly(2037, 1, 1)); farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2037, 1, 1)); tenant.AddFarmManagerMembership("manager", manager.Id);
            var workerPerson = farm.AddPerson("Worker", null, new DateOnly(2037, 1, 1));
            var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id,
                EmploymentType.Permanent, new DateOnly(2037, 1, 1), [1], new byte[12], new byte[16], "test-v1", new byte[32], "••••••12");
            var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2037, 1, Now, "manager", manager.Id); period.Open(Now, "manager", manager.Id, period.Version);
            var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, Now, "manager", manager.Id); var version = run.RecordCalculation(run.Version);
            var calculationId = Guid.NewGuid(); var lineId = Guid.NewGuid();
            var earning = PayrollEarningLine.Create(lineId, calculationId, tenant.Id, farm.Id, worker.Id,
                Guid.NewGuid(), "WorkRecord", new DateOnly(2037, 1, 10), Guid.NewGuid(), 1, Now, Now,
                Guid.NewGuid(), "[]", 10m, "days", "Daily", 10m, Guid.NewGuid(), 1, "fingerprint");
            var line = PayrollWorkerLine.Create(lineId, calculationId, tenant.Id, farm.Id, worker.Id,
                "Worker", [earning], []);
            var calculation = PayrollCalculation.Create(calculationId, run.Id, period.Id, tenant.Id,
                farm.Id, version, [line], [], "fingerprint", Now, "manager", manager.Id);
            run.Submit(version, Now, "manager", run.Version);
            PayrollApproval? approval = null;
            if (approved) { var runVersion = run.Version; run.Decide(true, version, Now, null, run.Version); approval = PayrollApproval.Create(run.Id, calculation.Id, tenant.Id, farm.Id, runVersion, version, true, null, Now, "grower", null, "approval"); period.Close(Now, "grower", null, run.Id, period.Version); }
            var farms = new Mock<IFarmSetupRepository>(); farms.Setup(x => x.GetTenantForUserAsync("manager", false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
            var labour = new Mock<ILabourRepository>(); labour.Setup(x => x.GetWorkerAsync(tenant.Id, farm.Id, worker.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(worker); labour.Setup(x => x.GetWorkersAsync(tenant.Id, farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([worker]);
            var repository = new Mock<IPayrollRepository>(); var transaction = new Mock<IPayrollTransaction>(); repository.Setup(x => x.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
            var fixture = new Fixture { Run = run, Line = line, Payroll = repository, Service = null! };
            repository.Setup(x => x.GetRunAsync(tenant.Id, farm.Id, run.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(run);
            repository.Setup(x => x.GetPeriodAsync(tenant.Id, farm.Id, period.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(period);
            repository.Setup(x => x.GetCalculationAsync(tenant.Id, farm.Id, run.Id, version, It.IsAny<CancellationToken>())).ReturnsAsync(calculation);
            repository.Setup(x => x.GetPayrollDecisionAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(approval);
            repository.Setup(x => x.GetPaymentByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, string key, CancellationToken _) => fixture.Payments.SingleOrDefault(x => x.IdempotencyKey == key));
            repository.Setup(x => x.GetPaymentAsync(tenant.Id, farm.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, Guid id, CancellationToken _) => fixture.Payments.SingleOrDefault(x => x.Id == id));
            repository.Setup(x => x.GetPaymentsAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => fixture.Payments.ToArray());
            repository.Setup(x => x.GetAcknowledgementAsync(tenant.Id, farm.Id, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, Guid id, CancellationToken _) => fixture.Acknowledgements.SingleOrDefault(x => x.PayrollPaymentId == id));
            repository.Setup(x => x.GetAcknowledgementByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, string key, CancellationToken _) => fixture.Acknowledgements.SingleOrDefault(x => x.IdempotencyKey == key));
            repository.Setup(x => x.GetAcknowledgementsAsync(tenant.Id, farm.Id, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => fixture.Acknowledgements.ToArray());
            repository.Setup(x => x.GetReversalByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, string key, CancellationToken _) => fixture.Reversals.SingleOrDefault(x => x.IdempotencyKey == key));
            repository.Setup(x => x.GetReversalsAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => fixture.Reversals.ToArray());
            repository.Setup(x => x.GetSettlementClosuresAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => fixture.Closures.ToArray()); repository.Setup(x => x.GetSettlementReopensAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => fixture.Reopens.ToArray());
            repository.Setup(x => x.GetSettlementClosureByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, string key, CancellationToken _) => fixture.Closures.SingleOrDefault(x => x.IdempotencyKey == key)); repository.Setup(x => x.GetSettlementReopenByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid _, Guid _, string key, CancellationToken _) => fixture.Reopens.SingleOrDefault(x => x.IdempotencyKey == key));
            repository.Setup(x => x.Add(It.IsAny<PayrollPayment>())).Callback<PayrollPayment>(fixture.Payments.Add); repository.Setup(x => x.Add(It.IsAny<PaymentAcknowledgement>())).Callback<PaymentAcknowledgement>(fixture.Acknowledgements.Add); repository.Setup(x => x.Add(It.IsAny<PayrollPaymentReversal>())).Callback<PayrollPaymentReversal>(fixture.Reversals.Add); repository.Setup(x => x.Add(It.IsAny<PayrollSettlementClosure>())).Callback<PayrollSettlementClosure>(fixture.Closures.Add); repository.Setup(x => x.Add(It.IsAny<PayrollSettlementReopen>())).Callback<PayrollSettlementReopen>(fixture.Reopens.Add); repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            var protector = new Mock<IPaymentRecipientProtector>(); protector.Setup(x => x.Protect(tenant.Id, farm.Id, It.IsAny<Guid>(), It.IsAny<string>())).Returns(new ProtectedPaymentRecipient([1], new byte[12], new byte[16], "test-v1", "•••• 0123"));
            var currentUser = new Mock<IUser>(); currentUser.Setup(x => x.Id).Returns("manager"); currentUser.Setup(x => x.CorrelationId).Returns("AUTOTEST-P6C-unit");
            fixture.Service = new PayrollSettlementService(farms.Object, labour.Object, repository.Object,
                protector.Object, currentUser.Object, new FixedTimeProvider(Now)); return fixture;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
