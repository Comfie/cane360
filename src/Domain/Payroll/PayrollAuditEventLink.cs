namespace Cane360.Domain.Payroll;

public sealed class PayrollAuditEventLink : BaseEntity
{
    private PayrollAuditEventLink() { }

    private PayrollAuditEventLink(Guid auditEventId, Guid tenantId, Guid farmId, Guid? payrollPeriodId, Guid? workerAdvanceId, Guid? advanceApprovalId, Guid? advanceIssueId)
    {
        AuditEventId = auditEventId; TenantId = tenantId; FarmId = farmId; PayrollPeriodId = payrollPeriodId; WorkerAdvanceId = workerAdvanceId; AdvanceApprovalId = advanceApprovalId; AdvanceIssueId = advanceIssueId;
    }

    public Guid AuditEventId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? PayrollPeriodId { get; private set; }
    public Guid? WorkerAdvanceId { get; private set; }
    public Guid? AdvanceApprovalId { get; private set; }
    public Guid? AdvanceIssueId { get; private set; }

    public static PayrollAuditEventLink ForPeriod(Guid auditEventId, Guid tenantId, Guid farmId, Guid payrollPeriodId) => new(auditEventId, tenantId, farmId, payrollPeriodId, null, null, null);
    public static PayrollAuditEventLink ForAdvance(Guid auditEventId, Guid tenantId, Guid farmId, Guid workerAdvanceId) => new(auditEventId, tenantId, farmId, null, workerAdvanceId, null, null);
    public static PayrollAuditEventLink ForApproval(Guid auditEventId, Guid tenantId, Guid farmId, Guid advanceApprovalId) => new(auditEventId, tenantId, farmId, null, null, advanceApprovalId, null);
    public static PayrollAuditEventLink ForIssue(Guid auditEventId, Guid tenantId, Guid farmId, Guid advanceIssueId) => new(auditEventId, tenantId, farmId, null, null, null, advanceIssueId);
}
