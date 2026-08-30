using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

internal static class PayrollAudit
{
    public static void Period(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, PayrollPeriod period, string action, DateTimeOffset at, string? reason, string summary)
    {
        var audit = Create(tenant, farm, user, nameof(PayrollPeriod), period.Id, action, at, reason, summary); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForPeriod(audit.Id, tenant.Id, farm.Id, period.Id));
    }

    public static void Advance(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, WorkerAdvance advance, string action, DateTimeOffset at, string? reason, string summary)
    {
        var audit = Create(tenant, farm, user, nameof(WorkerAdvance), advance.Id, action, at, reason, summary); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForAdvance(audit.Id, tenant.Id, farm.Id, advance.Id));
    }

    public static void Approval(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, WorkerAdvance advance, AdvanceApproval approval, string action, DateTimeOffset at, string? reason, string summary)
    {
        var audit = Create(tenant, farm, user, nameof(WorkerAdvance), advance.Id, action, at, reason, summary); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForApproval(audit.Id, tenant.Id, farm.Id, approval.Id));
    }

    public static void Issue(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, WorkerAdvance advance, AdvanceIssue issue, DateTimeOffset at, string summary)
    {
        var audit = Create(tenant, farm, user, nameof(WorkerAdvance), advance.Id, "AdvanceIssued", at, null, summary); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForIssue(audit.Id, tenant.Id, farm.Id, issue.Id));
    }

    public static void Run(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, PayrollRun run, string action, DateTimeOffset at, string? reason, string summary)
    {
        var audit = Create(tenant, farm, user, nameof(PayrollRun), run.Id, action, at, reason, summary); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForRun(audit.Id, tenant.Id, farm.Id, run.Id));
    }

    public static void Calculation(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, PayrollRun run, PayrollCalculation calculation, DateTimeOffset at)
    {
        var audit = Create(tenant, farm, user, nameof(PayrollRun), run.Id, "PayrollCalculated", at, null, $"Immutable payroll calculation version {calculation.CalculationVersion} created from authoritative evidence."); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForCalculation(audit.Id, tenant.Id, farm.Id, calculation.Id));
    }

    public static void PayrollDecision(IPayrollRepository repository, Tenant tenant, Farm farm, IUser user, PayrollRun run, PayrollApproval approval, DateTimeOffset at)
    {
        var audit = Create(tenant, farm, user, nameof(PayrollRun), run.Id, approval.Approved ? "PayrollApproved" : "PayrollRejected", at, approval.Reason, $"Grower decided immutable payroll calculation version {approval.CalculationVersion}."); repository.Add(audit); repository.Add(PayrollAuditEventLink.ForPayrollApproval(audit.Id, tenant.Id, farm.Id, approval.Id));
    }

    private static AuditEvent Create(Tenant tenant, Farm farm, IUser user, string subjectType, Guid subjectId, string action, DateTimeOffset at, string? reason, string summary)
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        return AuditEvent.Create(tenant.Id, farm.Id, subjectType, subjectId, action, userId, PayrollAccess.Role(tenant, userId), PayrollAccess.OperationalPerson(tenant, userId), at, user.CorrelationId ?? Guid.NewGuid().ToString("N"), reason, summary);
    }
}
