using Cane360.Domain.Payroll;

namespace Cane360.Application.Common.Interfaces;

public interface IPayrollRepository
{
    Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<PayrollPeriod?> GetPeriodAsync(Guid tenantId, Guid farmId, Guid periodId, bool trackChanges, CancellationToken cancellationToken);
    Task<WorkerAdvance?> GetAdvanceAsync(Guid tenantId, Guid farmId, Guid advanceId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<AdvanceIssue?> GetIssueByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken);
    Task<AdvanceApproval?> GetApprovalByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken);
    Task<AdvanceIssue?> GetIssueAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdvanceApproval>> GetApprovalsAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken);
    Task<bool> HasApprovedOrIssuedInstallmentAsync(Guid tenantId, Guid farmId, Guid periodId, CancellationToken cancellationToken);
    void Add(PayrollPeriod period);
    void Add(WorkerAdvance advance);
    void RemoveDraftInstallments(IReadOnlyCollection<AdvanceInstallment> installments);
    void Add(AdvanceApproval approval);
    void Add(AdvanceIssue issue);
    void Add(PayrollAuditEventLink auditLink);
    void Add(Cane360.Domain.Auditing.AuditEvent auditEvent);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
