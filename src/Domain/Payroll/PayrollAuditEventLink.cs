namespace Cane360.Domain.Payroll;

public sealed class PayrollAuditEventLink : BaseEntity
{
    private PayrollAuditEventLink() { }

    private PayrollAuditEventLink(Guid auditEventId, Guid tenantId, Guid farmId, Guid? payrollPeriodId, Guid? workerAdvanceId, Guid? advanceApprovalId, Guid? advanceIssueId, Guid? payrollRunId, Guid? payrollCalculationId, Guid? payrollApprovalId, Guid? payrollPaymentId, Guid? paymentAcknowledgementId, Guid? payrollPaymentReversalId, Guid? payrollSettlementClosureId, Guid? payrollSettlementReopenId)
    {
        AuditEventId = auditEventId; TenantId = tenantId; FarmId = farmId; PayrollPeriodId = payrollPeriodId; WorkerAdvanceId = workerAdvanceId; AdvanceApprovalId = advanceApprovalId; AdvanceIssueId = advanceIssueId; PayrollRunId = payrollRunId; PayrollCalculationId = payrollCalculationId; PayrollApprovalId = payrollApprovalId; PayrollPaymentId = payrollPaymentId; PaymentAcknowledgementId = paymentAcknowledgementId; PayrollPaymentReversalId = payrollPaymentReversalId; PayrollSettlementClosureId = payrollSettlementClosureId; PayrollSettlementReopenId = payrollSettlementReopenId;
    }

    public Guid AuditEventId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? PayrollPeriodId { get; private set; }
    public Guid? WorkerAdvanceId { get; private set; }
    public Guid? AdvanceApprovalId { get; private set; }
    public Guid? AdvanceIssueId { get; private set; }
    public Guid? PayrollRunId { get; private set; }
    public Guid? PayrollCalculationId { get; private set; }
    public Guid? PayrollApprovalId { get; private set; }
    public Guid? PayrollPaymentId { get; private set; }
    public Guid? PaymentAcknowledgementId { get; private set; }
    public Guid? PayrollPaymentReversalId { get; private set; }
    public Guid? PayrollSettlementClosureId { get; private set; }
    public Guid? PayrollSettlementReopenId { get; private set; }

    private static PayrollAuditEventLink Create(Guid audit, Guid tenant, Guid farm, Guid? period = null, Guid? advance = null, Guid? advanceApproval = null, Guid? issue = null, Guid? run = null, Guid? calculation = null, Guid? payrollApproval = null, Guid? payment = null, Guid? acknowledgement = null, Guid? reversal = null, Guid? closure = null, Guid? reopen = null) => new(audit, tenant, farm, period, advance, advanceApproval, issue, run, calculation, payrollApproval, payment, acknowledgement, reversal, closure, reopen);
    public static PayrollAuditEventLink ForPeriod(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, period: id);
    public static PayrollAuditEventLink ForAdvance(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, advance: id);
    public static PayrollAuditEventLink ForApproval(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, advanceApproval: id);
    public static PayrollAuditEventLink ForIssue(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, issue: id);
    public static PayrollAuditEventLink ForRun(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, run: id);
    public static PayrollAuditEventLink ForCalculation(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, calculation: id);
    public static PayrollAuditEventLink ForPayrollApproval(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, payrollApproval: id);
    public static PayrollAuditEventLink ForPayment(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, payment: id);
    public static PayrollAuditEventLink ForAcknowledgement(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, acknowledgement: id);
    public static PayrollAuditEventLink ForReversal(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, reversal: id);
    public static PayrollAuditEventLink ForSettlementClosure(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, closure: id);
    public static PayrollAuditEventLink ForSettlementReopen(Guid audit, Guid tenant, Guid farm, Guid id) => Create(audit, tenant, farm, reopen: id);
}
