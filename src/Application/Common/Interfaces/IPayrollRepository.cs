using Cane360.Domain.Payroll;

namespace Cane360.Application.Common.Interfaces;

public interface IPayrollRepository
{
    Task<IPayrollTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<PayrollPeriod?> GetPeriodAsync(Guid tenantId, Guid farmId, Guid periodId, bool trackChanges, CancellationToken cancellationToken);
    Task<WorkerAdvance?> GetAdvanceAsync(Guid tenantId, Guid farmId, Guid advanceId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<AdvanceIssue?> GetIssueByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken);
    Task<AdvanceApproval?> GetApprovalByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken);
    Task<AdvanceIssue?> GetIssueAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdvanceApproval>> GetApprovalsAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken);
    Task<bool> HasApprovedOrIssuedInstallmentAsync(Guid tenantId, Guid farmId, Guid periodId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollRun>> GetRunsAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<PayrollRun?> GetRunAsync(Guid tenantId, Guid farmId, Guid runId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollCalculation>> GetCalculationsAsync(Guid tenantId, Guid farmId, Guid runId, CancellationToken cancellationToken);
    Task<PayrollCalculation?> GetCalculationAsync(Guid tenantId, Guid farmId, Guid runId, int calculationVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdvanceRecovery>> GetRecoveriesAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetConsumedEvidenceIdsAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<PayrollApproval?> GetPayrollApprovalByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken);
    Task<PayrollApproval?> GetPayrollDecisionAsync(Guid tenantId, Guid farmId, Guid runId, CancellationToken cancellationToken);
    void Add(PayrollPeriod period);
    void Add(WorkerAdvance advance);
    void RemoveDraftInstallments(IReadOnlyCollection<AdvanceInstallment> installments);
    void Add(AdvanceApproval approval);
    void Add(AdvanceIssue issue);
    void Add(PayrollAuditEventLink auditLink);
    void Add(Cane360.Domain.Auditing.AuditEvent auditEvent);
    void Add(PayrollRun run);
    void Add(PayrollCalculation calculation);
    void Add(PayrollApproval approval);
    void Add(PayrollEvidenceConsumption consumption);
    void Add(AdvanceRecovery recovery);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
