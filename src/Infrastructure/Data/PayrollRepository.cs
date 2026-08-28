using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Auditing;
using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class PayrollRepository(ApplicationDbContext context) : IPayrollRepository
{
    public async Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) => await Track(context.PayrollPeriods.Where(x => x.TenantId == tenantId && x.FarmId == farmId), trackChanges).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync(cancellationToken);
    public Task<PayrollPeriod?> GetPeriodAsync(Guid tenantId, Guid farmId, Guid periodId, bool trackChanges, CancellationToken cancellationToken) => Track(context.PayrollPeriods.Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.Id == periodId), trackChanges).SingleOrDefaultAsync(cancellationToken);
    public Task<WorkerAdvance?> GetAdvanceAsync(Guid tenantId, Guid farmId, Guid advanceId, bool trackChanges, CancellationToken cancellationToken) => Include(Track(context.WorkerAdvances.Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.Id == advanceId), trackChanges)).SingleOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<WorkerAdvance>> GetAdvancesAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) => await Include(Track(context.WorkerAdvances.Where(x => x.TenantId == tenantId && x.FarmId == farmId), trackChanges)).OrderByDescending(x => x.RequestedAt).ToListAsync(cancellationToken);
    public Task<AdvanceIssue?> GetIssueByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken) => context.AdvanceIssues.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId && x.IdempotencyKey == idempotencyKey, cancellationToken);
    public Task<AdvanceApproval?> GetApprovalByKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey, CancellationToken cancellationToken) => context.AdvanceApprovals.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId && x.IdempotencyKey == idempotencyKey, cancellationToken);
    public Task<AdvanceIssue?> GetIssueAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken) => context.AdvanceIssues.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId && x.WorkerAdvanceId == advanceId, cancellationToken);
    public async Task<IReadOnlyList<AdvanceApproval>> GetApprovalsAsync(Guid tenantId, Guid farmId, Guid advanceId, CancellationToken cancellationToken) => await context.AdvanceApprovals.AsNoTracking().Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.WorkerAdvanceId == advanceId).OrderBy(x => x.DecidedAt).ToListAsync(cancellationToken);
    public Task<bool> HasApprovedOrIssuedInstallmentAsync(Guid tenantId, Guid farmId, Guid periodId, CancellationToken cancellationToken) => context.AdvanceInstallments.AnyAsync(x => x.TenantId == tenantId && x.FarmId == farmId && x.PayrollPeriodId == periodId && context.WorkerAdvances.Any(a => a.Id == x.WorkerAdvanceId && (a.Status == AdvanceStatus.Approved || a.Status == AdvanceStatus.Issued)), cancellationToken);
    public void Add(PayrollPeriod period) => context.PayrollPeriods.Add(period); public void Add(WorkerAdvance advance) => context.WorkerAdvances.Add(advance);
    public void RemoveDraftInstallments(IReadOnlyCollection<AdvanceInstallment> installments)
    {
        foreach (var installment in installments)
        {
            context.Entry(installment).State = EntityState.Deleted;
        }
    }
    public void Add(AdvanceApproval approval) => context.AdvanceApprovals.Add(approval); public void Add(AdvanceIssue issue) => context.AdvanceIssues.Add(issue); public void Add(PayrollAuditEventLink auditLink) => context.PayrollAuditEventLinks.Add(auditLink); public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { return await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("This payroll record changed before the action could be completed. Refresh and try again."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { ConstraintName: "UX_PayrollPeriods_Farm_Year_Month" }) { throw new ConflictException("A payroll period already exists for this farm and calendar month."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.ConstraintName?.Contains("AdvanceApprovals", StringComparison.Ordinal) == true) { throw new ConflictException("This exact advance version already has an authoritative Grower decision."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.ConstraintName?.Contains("AdvanceIssues", StringComparison.Ordinal) == true) { throw new ConflictException("This advance issue was already recorded."); }
    }
    private static IQueryable<T> Track<T>(IQueryable<T> query, bool track) where T : class => track ? query : query.AsNoTracking();
    private static IQueryable<WorkerAdvance> Include(IQueryable<WorkerAdvance> query) => query.Include(x => x.Installments);
}
