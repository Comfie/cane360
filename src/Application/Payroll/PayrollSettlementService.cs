using System.Text.Json;
using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Payroll;

public sealed class PayrollSettlementService(IFarmSetupRepository farms, ILabourRepository labour,
    IPayrollRepository payroll, IPaymentRecipientProtector recipientProtector, IUser user,
    TimeProvider clock) : IPayrollSettlementService
{
    public async Task<RunSettlementDto> GetRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken);
        var exact = await ExactApprovedAsync(context, runId, null, cancellationToken);
        return await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
    }

    public async Task<WorkerSettlementDto> GetWorkerAsync(Guid runId, int calculationVersion,
        Guid workerLineId, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken);
        var exact = await ExactApprovedAsync(context, runId, calculationVersion, cancellationToken);
        var summary = await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
        return summary.Workers.SingleOrDefault(x => x.PayrollWorkerLineId == workerLineId)
            ?? throw new NotFoundException(workerLineId.ToString(), "Payroll worker line");
    }

    public async Task<PayrollPaymentDto> RecordPaymentAsync(Guid runId,
        RecordPayrollPaymentInput input, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken); RequireOperator(context);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var existing = await payroll.GetPaymentByKeyAsync(context.Tenant.Id, context.Farm.Id,
            input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayrollRunId != runId || existing.CalculationVersion != input.CalculationVersion ||
                existing.PayrollWorkerLineId != input.PayrollWorkerLineId || existing.AmountUsd != input.AmountUsd ||
                !string.Equals(existing.Method.ToString(), input.Method, StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("This payment idempotency key is bound to different payment evidence.");
            var mapped = await PaymentAsync(context, existing, cancellationToken);
            await transaction.CommitAsync(cancellationToken); return mapped;
        }
        var exact = await ExactApprovedAsync(context, runId, input.CalculationVersion, cancellationToken);
        await RequireOpenSettlementAsync(context, runId, cancellationToken);
        var line = exact.Calculation.WorkerLines.SingleOrDefault(x => x.Id == input.PayrollWorkerLineId)
            ?? throw new NotFoundException(input.PayrollWorkerLineId.ToString(), "Payroll worker line");
        var current = await WorkerTotalsAsync(context, runId, line, cancellationToken);
        var method = ParseMethod(input.Method);
        var contributes = method == PayrollPaymentMethod.Cash || input.ExternalStatus is "Posted" or "Successful";
        if (contributes && current.Paid + input.AmountUsd > line.NetAmountUsd)
            Fail(nameof(input.AmountUsd), "Payment cannot exceed the worker's approved outstanding net pay.");
        var now = clock.GetUtcNow(); var id = Guid.NewGuid(); var correlation = Correlation();
        PayrollPayment? payment = null;
        if (method == PayrollPaymentMethod.Cash)
            Domain(() => payment = PayrollPayment.Cash(id, context.Tenant.Id, context.Farm.Id, runId,
                exact.Calculation.Id, exact.Calculation.CalculationVersion, line.Id, line.WorkerProfileId,
                input.AmountUsd, input.PaymentDate, context.UserId, context.PersonId, now,
                input.IdempotencyKey, correlation), nameof(input.AmountUsd));
        else
        {
            var protectedRecipient = recipientProtector.Protect(context.Tenant.Id, context.Farm.Id,
                id, input.RecipientNumber ?? string.Empty);
            Domain(() => payment = PayrollPayment.MobileMoney(id, context.Tenant.Id, context.Farm.Id,
                runId, exact.Calculation.Id, exact.Calculation.CalculationVersion, line.Id,
                line.WorkerProfileId, input.AmountUsd, input.PaymentDate, input.ExternalStatus ?? string.Empty,
                input.Provider ?? string.Empty, protectedRecipient.Ciphertext, protectedRecipient.Nonce,
                protectedRecipient.Tag, protectedRecipient.KeyId, protectedRecipient.DisplayMask,
                input.TransactionReference ?? string.Empty, context.UserId, context.PersonId, now,
                input.IdempotencyKey, correlation), nameof(input.Method));
        }
        payroll.Add(payment!); Audit(context, payment!.Id, "PayrollPayment", "PaymentRecorded", now,
            null, $"{payment.Method} payroll payment evidence recorded for exact calculation version {payment.CalculationVersion}; no external payment was executed.",
            link => PayrollAuditEventLink.ForPayment(link.Id, context.Tenant.Id, context.Farm.Id, payment.Id));
        await payroll.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return await PaymentAsync(context, payment, cancellationToken);
    }

    public async Task<PayrollPaymentDto> AcknowledgeAsync(Guid paymentId,
        RecordPaymentAcknowledgementInput input, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken); RequireOperator(context);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var byKey = await payroll.GetAcknowledgementByKeyAsync(context.Tenant.Id, context.Farm.Id,
            input.IdempotencyKey, cancellationToken);
        if (byKey is not null)
        {
            if (byKey.PayrollPaymentId != paymentId || byKey.Status != input.Status)
                throw new ConflictException("This acknowledgement idempotency key is bound to different evidence.");
            var priorPayment = await RequirePaymentAsync(context, paymentId, cancellationToken);
            var priorDto = await PaymentAsync(context, priorPayment, cancellationToken);
            await transaction.CommitAsync(cancellationToken); return priorDto;
        }
        var payment = await RequirePaymentAsync(context, paymentId, cancellationToken);
        await RequireOpenSettlementAsync(context, payment.PayrollRunId, cancellationToken);
        if (await payroll.GetAcknowledgementAsync(context.Tenant.Id, context.Farm.Id, paymentId, cancellationToken) is not null)
            throw new ConflictException("This payment already has authoritative acknowledgement evidence.");
        if (input.AcknowledgedByPersonId is not null && context.Farm.Persons.All(x => x.Id != input.AcknowledgedByPersonId))
            Fail(nameof(input.AcknowledgedByPersonId), "The acknowledging person must belong to this farm.");
        var now = clock.GetUtcNow(); PaymentAcknowledgement? acknowledgement = null;
        Domain(() => acknowledgement = PaymentAcknowledgement.Create(paymentId, context.Tenant.Id,
            context.Farm.Id, input.Status, input.AcknowledgedByPersonId, context.UserId,
            context.PersonId, input.AcknowledgedAt.ToUniversalTime(), input.EvidenceReference, now,
            input.IdempotencyKey, Correlation()), nameof(input.Status));
        payroll.Add(acknowledgement!); Audit(context, acknowledgement!.Id, "PaymentAcknowledgement",
            "PaymentAcknowledgementRecorded", now, null, "Operational payment acknowledgement captured by an authenticated user for a distinct acknowledging person.",
            link => PayrollAuditEventLink.ForAcknowledgement(link.Id, context.Tenant.Id, context.Farm.Id, acknowledgement.Id));
        await payroll.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return await PaymentAsync(context, payment, cancellationToken);
    }

    public async Task<PayrollPaymentDto> ReverseAsync(Guid paymentId,
        ReversePayrollPaymentInput input, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken); RequireOperator(context);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var existing = await payroll.GetReversalByKeyAsync(context.Tenant.Id, context.Farm.Id,
            input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayrollPaymentId != paymentId || existing.AmountUsd != input.AmountUsd)
                throw new ConflictException("This reversal idempotency key is bound to a different correction.");
            var priorPayment = await RequirePaymentAsync(context, paymentId, cancellationToken);
            var priorDto = await PaymentAsync(context, priorPayment, cancellationToken);
            await transaction.CommitAsync(cancellationToken); return priorDto;
        }
        var payment = await RequirePaymentAsync(context, paymentId, cancellationToken);
        await RequireOpenSettlementAsync(context, payment.PayrollRunId, cancellationToken);
        if (!payment.ContributesToPaidAmount) Fail(nameof(paymentId), "A non-posted payment does not contribute to settlement and cannot be reversed.");
        var reversals = await payroll.GetReversalsAsync(context.Tenant.Id, context.Farm.Id,
            payment.PayrollRunId, cancellationToken);
        var alreadyReversed = reversals.Where(x => x.PayrollPaymentId == paymentId).Sum(x => x.AmountUsd);
        if (input.AmountUsd <= 0 || alreadyReversed + input.AmountUsd > payment.AmountUsd)
            Fail(nameof(input.AmountUsd), "Reversal cannot exceed the payment's remaining unreversed amount.");
        var now = clock.GetUtcNow(); PayrollPaymentReversal? reversal = null;
        Domain(() => reversal = PayrollPaymentReversal.Create(payment, input.AmountUsd, input.Reason,
            context.UserId, context.PersonId, now, input.IdempotencyKey, Correlation()), nameof(input.AmountUsd));
        payroll.Add(reversal!); Audit(context, reversal!.Id, "PayrollPaymentReversal", "PaymentReversed",
            now, input.Reason, "Append-only payroll payment correction recorded; the original payment remains unchanged.",
            link => PayrollAuditEventLink.ForReversal(link.Id, context.Tenant.Id, context.Farm.Id, reversal.Id));
        await payroll.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return await PaymentAsync(context, payment, cancellationToken);
    }

    public async Task<RunSettlementDto> CloseAsync(Guid runId, ClosePayrollSettlementInput input,
        CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken); RequireOperator(context);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var existing = await payroll.GetSettlementClosureByKeyAsync(context.Tenant.Id, context.Farm.Id,
            input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayrollRunId != runId || existing.CalculationVersion != input.CalculationVersion)
                throw new ConflictException("This settlement-close idempotency key is bound to a different exact version.");
            var prior = await ExactApprovedAsync(context, runId, input.CalculationVersion, cancellationToken);
            var priorSummary = await SummaryAsync(context, prior.Run, prior.Calculation, cancellationToken);
            await transaction.CommitAsync(cancellationToken); return priorSummary;
        }
        var exact = await ExactApprovedAsync(context, runId, input.CalculationVersion, cancellationToken);
        await RequireOpenSettlementAsync(context, runId, cancellationToken);
        var summary = await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
        if (!summary.CanClose || summary.OutstandingAmountUsd != 0 || summary.AcknowledgementExceptions != 0)
            Fail(nameof(runId), "Final settlement requires every worker to be fully settled and every active cash payment to be acknowledged.");
        var closures = await payroll.GetSettlementClosuresAsync(context.Tenant.Id, context.Farm.Id,
            runId, cancellationToken); var now = clock.GetUtcNow();
        var closure = PayrollSettlementClosure.Create(context.Tenant.Id, context.Farm.Id, exact.Run,
            exact.Calculation, closures.Count + 1, summary.PaidAmountUsd, now, context.UserId,
            context.PersonId, input.IdempotencyKey, Correlation());
        payroll.Add(closure); Audit(context, closure.Id, "PayrollSettlementClosure", "SettlementClosed",
            now, null, $"Settlement closed for exact approved calculation version {closure.CalculationVersion} with reconciled immutable totals.",
            link => PayrollAuditEventLink.ForSettlementClosure(link.Id, context.Tenant.Id, context.Farm.Id, closure.Id));
        await payroll.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
    }

    public async Task<RunSettlementDto> ReopenAsync(Guid runId, ReopenPayrollSettlementInput input,
        CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken); PayrollAccess.RequireGrower(context.Tenant, context.UserId);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var existing = await payroll.GetSettlementReopenByKeyAsync(context.Tenant.Id, context.Farm.Id,
            input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayrollRunId != runId || existing.CalculationVersion != input.CalculationVersion)
                throw new ConflictException("This settlement-reopen idempotency key is bound to a different exact version.");
            var prior = await ExactApprovedAsync(context, runId, input.CalculationVersion, cancellationToken);
            var priorSummary = await SummaryAsync(context, prior.Run, prior.Calculation, cancellationToken);
            await transaction.CommitAsync(cancellationToken); return priorSummary;
        }
        var exact = await ExactApprovedAsync(context, runId, input.CalculationVersion, cancellationToken);
        var closures = await payroll.GetSettlementClosuresAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        var reopens = await payroll.GetSettlementReopensAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        var active = closures.LastOrDefault(x => reopens.All(r => r.PayrollSettlementClosureId != x.Id))
            ?? throw new ConflictException("Payroll settlement is not closed.");
        var now = clock.GetUtcNow(); var reopen = PayrollSettlementReopen.Create(active, input.Reason,
            now, context.UserId, context.PersonId, input.IdempotencyKey, Correlation());
        payroll.Add(reopen); Audit(context, reopen.Id, "PayrollSettlementReopen", "SettlementReopened",
            now, input.Reason, "Grower-authorised payment-side settlement reopen recorded; approved calculation and labour evidence remain locked.",
            link => PayrollAuditEventLink.ForSettlementReopen(link.Id, context.Tenant.Id, context.Farm.Id, reopen.Id));
        await payroll.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
    }

    public async Task<OperationalPayslipDto> GetPayslipAsync(Guid runId, int calculationVersion,
        Guid workerLineId, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken);
        var exact = await ExactApprovedAsync(context, runId, calculationVersion, cancellationToken);
        var line = exact.Calculation.WorkerLines.SingleOrDefault(x => x.Id == workerLineId)
            ?? throw new NotFoundException(workerLineId.ToString(), "Payroll worker line");
        var worker = await labour.GetWorkerAsync(context.Tenant.Id, context.Farm.Id,
            line.WorkerProfileId, false, cancellationToken) ?? throw new NotFoundException(line.WorkerProfileId.ToString(), "Worker");
        var settlement = await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
        var workerSettlement = settlement.Workers.Single(x => x.PayrollWorkerLineId == line.Id);
        var now = clock.GetUtcNow(); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(
            context.Tenant.Id, context.Farm.Id, exact.Run.PayrollPeriodId, false, cancellationToken), exact.Run.PayrollPeriodId);
        Audit(context, exact.Calculation.Id, "PayrollCalculation", "PayslipGenerated", now, null,
            $"Operational payslip generated for one worker line from exact approved calculation version {calculationVersion}.",
            link => PayrollAuditEventLink.ForCalculation(link.Id, context.Tenant.Id, context.Farm.Id, exact.Calculation.Id));
        await payroll.SaveChangesAsync(cancellationToken);
        return new("Operational payroll record — not a statutory tax payslip", context.Farm.Name,
            period.DisplayName, runId, exact.Calculation.Id, calculationVersion, line.Id,
            line.WorkerNameSnapshot, worker.NationalIdMask,
            PayrollRunMapper.Map(exact.Calculation).Workers.Single(x => x.Id == line.Id).Earnings,
            line.GrossAmountUsd, line.DeductionAmountUsd, line.AdvanceDeductions.Sum(x => x.AmountUsd),
            line.NetAmountUsd, workerSettlement.ValidPaidAmountUsd, workerSettlement.OutstandingAmountUsd,
            workerSettlement.SettlementStatus, now, $"PAYSLIP-{runId:N}-V{calculationVersion}-{line.Id:N}");
    }

    public async Task<CashPaymentRegisterDto> GetCashRegisterAsync(Guid runId,
        int calculationVersion, CancellationToken cancellationToken)
    {
        var context = await ContextAsync(cancellationToken);
        var exact = await ExactApprovedAsync(context, runId, calculationVersion, cancellationToken);
        var summary = await SummaryAsync(context, exact.Run, exact.Calculation, cancellationToken);
        var workers = (await labour.GetWorkersAsync(context.Tenant.Id, context.Farm.Id, false,
            cancellationToken)).ToDictionary(x => x.Id);
        var rows = summary.Workers.Select(worker =>
        {
            var cash = worker.Payments.Where(x => x.Method == "Cash" && x.ActiveAmountUsd > 0).ToArray();
            return new CashPaymentRegisterRowDto(worker.PayrollWorkerLineId, worker.WorkerName,
                workers.GetValueOrDefault(worker.WorkerProfileId)?.NationalIdMask ?? "••••••",
                worker.ApprovedNetUsd, cash.Sum(x => x.ActiveAmountUsd),
                cash.Length == 0 ? null : cash.Max(x => x.PaymentDate),
                cash.Length == 0 ? "Not required" : cash.All(x => x.Acknowledgement?.Status == "Acknowledged") ? "Complete" : "Outstanding",
                worker.OutstandingAmountUsd);
        }).ToArray();
        var now = clock.GetUtcNow(); Audit(context, exact.Calculation.Id, "PayrollCalculation",
            "CashRegisterGenerated", now, null, $"Printable cash register generated from active payments for exact approved calculation version {calculationVersion}.",
            link => PayrollAuditEventLink.ForCalculation(link.Id, context.Tenant.Id, context.Farm.Id, exact.Calculation.Id));
        await payroll.SaveChangesAsync(cancellationToken);
        return new(context.Farm.Name, summary.PayrollPeriod, runId, exact.Calculation.Id,
            calculationVersion, rows, rows.Sum(x => x.ApprovedNetUsd), rows.Sum(x => x.CashAmountPaidUsd),
            rows.Sum(x => x.OutstandingAmountUsd), now, $"CASH-{runId:N}-V{calculationVersion}");
    }

    private async Task<RunSettlementDto> SummaryAsync(Context context, PayrollRun run,
        PayrollCalculation calculation, CancellationToken cancellationToken)
    {
        var payments = await payroll.GetPaymentsAsync(context.Tenant.Id, context.Farm.Id, run.Id, cancellationToken);
        var reversals = await payroll.GetReversalsAsync(context.Tenant.Id, context.Farm.Id, run.Id, cancellationToken);
        var acknowledgements = await payroll.GetAcknowledgementsAsync(context.Tenant.Id, context.Farm.Id,
            payments.Select(x => x.Id).ToArray(), cancellationToken);
        var closures = await payroll.GetSettlementClosuresAsync(context.Tenant.Id, context.Farm.Id, run.Id, cancellationToken);
        var reopens = await payroll.GetSettlementReopensAsync(context.Tenant.Id, context.Farm.Id, run.Id, cancellationToken);
        var activeClosed = closures.Any(x => reopens.All(r => r.PayrollSettlementClosureId != x.Id));
        var paymentDtos = payments.Select(payment => MapPayment(payment,
            acknowledgements.SingleOrDefault(x => x.PayrollPaymentId == payment.Id),
            reversals.Where(x => x.PayrollPaymentId == payment.Id).ToArray())).ToArray();
        var workers = calculation.WorkerLines.OrderBy(x => x.WorkerNameSnapshot).Select(line =>
        {
            var linePayments = paymentDtos.Where(x => x.PayrollWorkerLineId == line.Id).ToArray();
            var paid = linePayments.Sum(x => x.ActiveAmountUsd); var reversed = linePayments.Sum(x => x.ReversedAmountUsd);
            var outstanding = line.NetAmountUsd - paid;
            var cashActive = linePayments.Where(x => x.Method == "Cash" && x.ActiveAmountUsd > 0).ToArray();
            var ack = cashActive.All(x => x.Acknowledgement?.Status == "Acknowledged");
            var status = outstanding == line.NetAmountUsd && line.NetAmountUsd > 0 ? "Unpaid" : outstanding > 0 ? "PartPaid" : "Paid";
            return new WorkerSettlementDto(line.Id, line.WorkerProfileId, line.WorkerNameSnapshot,
                line.GrossAmountUsd, line.DeductionAmountUsd, line.NetAmountUsd, paid, reversed,
                outstanding, linePayments.Count(x => x.ActiveAmountUsd > 0),
                string.Join(", ", linePayments.Where(x => x.ActiveAmountUsd > 0).Select(x => x.Method).Distinct()),
                ack, status, linePayments);
        }).ToArray();
        var paidTotal = workers.Sum(x => x.ValidPaidAmountUsd); var outstandingTotal = workers.Sum(x => x.OutstandingAmountUsd);
        var status = activeClosed ? "Closed" : outstandingTotal == calculation.NetAmountUsd && calculation.NetAmountUsd > 0 ? "Unpaid" : outstandingTotal > 0 ? "PartPaid" : "Paid";
        var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(context.Tenant.Id,
            context.Farm.Id, run.PayrollPeriodId, false, cancellationToken), run.PayrollPeriodId);
        return new(run.Id, calculation.Id, calculation.CalculationVersion, context.Farm.Name,
            period.DisplayName, calculation.GrossAmountUsd, calculation.DeductionAmountUsd,
            calculation.NetAmountUsd, paidTotal, workers.Sum(x => x.ReversedAmountUsd), outstandingTotal,
            workers.Length, workers.Count(x => x.SettlementStatus == "Paid"), workers.Count(x => x.SettlementStatus != "Paid"),
            workers.Count(x => !x.AcknowledgementComplete), status, activeClosed,
            !activeClosed && outstandingTotal == 0 && workers.All(x => x.AcknowledgementComplete), workers);
    }

    private async Task<(decimal Paid, decimal Reversed)> WorkerTotalsAsync(Context context,
        Guid runId, PayrollWorkerLine line, CancellationToken cancellationToken)
    {
        var payments = await payroll.GetPaymentsAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        var reversals = await payroll.GetReversalsAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        var relevant = payments.Where(x => x.PayrollWorkerLineId == line.Id && x.ContributesToPaidAmount).ToArray();
        var reversed = reversals.Where(x => relevant.Any(p => p.Id == x.PayrollPaymentId)).Sum(x => x.AmountUsd);
        return (relevant.Sum(x => x.AmountUsd) - reversed, reversed);
    }

    private async Task<PayrollPaymentDto> PaymentAsync(Context context, PayrollPayment payment,
        CancellationToken cancellationToken)
    {
        var acknowledgement = await payroll.GetAcknowledgementAsync(context.Tenant.Id, context.Farm.Id,
            payment.Id, cancellationToken);
        var reversals = (await payroll.GetReversalsAsync(context.Tenant.Id, context.Farm.Id,
            payment.PayrollRunId, cancellationToken)).Where(x => x.PayrollPaymentId == payment.Id).ToArray();
        return MapPayment(payment, acknowledgement, reversals);
    }

    private static PayrollPaymentDto MapPayment(PayrollPayment payment,
        PaymentAcknowledgement? acknowledgement, IReadOnlyList<PayrollPaymentReversal> reversals)
    {
        var reversed = reversals.Sum(x => x.AmountUsd); var active = payment.ContributesToPaidAmount ? payment.AmountUsd - reversed : 0;
        return new(payment.Id, payment.PayrollRunId, payment.PayrollCalculationId,
            payment.CalculationVersion, payment.PayrollWorkerLineId, payment.WorkerProfileId,
            payment.Method.ToString(), payment.AmountUsd, payment.PaymentDate, payment.ExternalStatus,
            payment.Provider, payment.MaskedRecipientNumber, payment.TransactionReference,
            payment.RecordedByUserId, payment.RecordedByPersonId, payment.CreatedAt, reversed, active,
            acknowledgement is null ? null : new(acknowledgement.Id, acknowledgement.Status,
                acknowledgement.AcknowledgedByPersonId, acknowledgement.CapturedByUserId,
                acknowledgement.CapturedByPersonId, acknowledgement.AcknowledgedAt,
                acknowledgement.EvidenceReference, acknowledgement.CreatedAt),
            reversals.Select(x => new PayrollPaymentReversalDto(x.Id, x.AmountUsd, x.Reason,
                x.ReversedByUserId, x.ReversedByPersonId, x.ReversedAt)).ToArray());
    }

    private async Task<Exact> ExactApprovedAsync(Context context, Guid runId, int? version,
        CancellationToken cancellationToken)
    {
        var run = PayrollAccess.RequireRun(await payroll.GetRunAsync(context.Tenant.Id, context.Farm.Id,
            runId, false, cancellationToken), runId);
        if (run.Status != PayrollRunStatus.Approved || run.SubmittedCalculationVersion is null)
            throw new ConflictException("Payroll payments require an authoritative Grower-approved calculation.");
        if (version is not null && version != run.SubmittedCalculationVersion)
            throw new ConflictException("The exact Grower-approved calculation version is required.");
        var calculation = await payroll.GetCalculationAsync(context.Tenant.Id, context.Farm.Id,
            runId, run.SubmittedCalculationVersion.Value, cancellationToken)
            ?? throw new NotFoundException(run.SubmittedCalculationVersion.Value.ToString(), "Approved payroll calculation");
        var approval = await payroll.GetPayrollDecisionAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        if (approval is null || !approval.Approved || approval.PayrollCalculationId != calculation.Id ||
            approval.CalculationVersion != calculation.CalculationVersion)
            throw new ConflictException("The exact Grower-approved payroll identity could not be revalidated.");
        return new(run, calculation);
    }

    private async Task RequireOpenSettlementAsync(Context context, Guid runId,
        CancellationToken cancellationToken)
    {
        var closures = await payroll.GetSettlementClosuresAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        var reopens = await payroll.GetSettlementReopensAsync(context.Tenant.Id, context.Farm.Id, runId, cancellationToken);
        if (closures.Any(x => reopens.All(r => r.PayrollSettlementClosureId != x.Id)))
            throw new ConflictException("Payroll settlement is closed. A Grower-authorised reopen is required for payment-side correction.");
    }

    private async Task<PayrollPayment> RequirePaymentAsync(Context context, Guid paymentId,
        CancellationToken cancellationToken) => await payroll.GetPaymentAsync(context.Tenant.Id,
        context.Farm.Id, paymentId, cancellationToken) ?? throw new NotFoundException(paymentId.ToString(), "Payroll payment");

    private async Task<Context> ContextAsync(CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        return new(tenant, farm, userId, PayrollAccess.OperationalPerson(tenant, userId));
    }

    private static void RequireOperator(Context context)
    {
        if (PayrollAccess.Role(context.Tenant, context.UserId) is not (TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager))
            throw new ForbiddenAccessException();
    }

    private void Audit(Context context, Guid subjectId, string subjectType, string action,
        DateTimeOffset at, string? reason, string summary,
        Func<AuditEvent, PayrollAuditEventLink> linkFactory)
    {
        var audit = AuditEvent.Create(context.Tenant.Id, context.Farm.Id, subjectType, subjectId,
            action, context.UserId, PayrollAccess.Role(context.Tenant, context.UserId), context.PersonId,
            at, Correlation(), reason, summary); payroll.Add(audit); payroll.Add(linkFactory(audit));
    }

    private string Correlation() => user.CorrelationId ?? Guid.NewGuid().ToString("N");
    private static PayrollPaymentMethod ParseMethod(string method) => Enum.TryParse(method, true,
        out PayrollPaymentMethod parsed) ? parsed : throw new Cane360.Application.Common.Exceptions.ValidationException(
            [new FluentValidation.Results.ValidationFailure(nameof(RecordPayrollPaymentInput.Method), "Payment method must be Cash or MobileMoney.")]);
    private static void Domain(Action action, string property) { try { action(); } catch (ArgumentException exception) { Fail(property, exception.Message); } catch (InvalidOperationException exception) { Fail(property, exception.Message); } }
    private static void Fail(string property, string message) => throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(property, message)]);
    private sealed record Context(Tenant Tenant, Farm Farm, string UserId, Guid? PersonId);
    private sealed record Exact(PayrollRun Run, PayrollCalculation Calculation);
}
