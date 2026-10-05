using FluentValidation.Results;
using ValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Payroll;

internal static class PayrollAccess
{
    public static async Task<(Tenant Tenant, Farm Farm, string UserId)> ContextAsync(IFarmSetupRepository farms,
        IUser user, bool track, CancellationToken cancellationToken, bool includeOperationalHistory = true)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        Tenant tenant = await (includeOperationalHistory
                            ? farms.GetTenantForUserAsync(userId, track, cancellationToken)
                            : farms.GetTenantReferenceContextForUserAsync(userId, track, cancellationToken))
                        ?? throw new NotFoundException(userId, "Active grower or farm-manager membership");
        return (tenant, tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm"), userId);
    }

    public static Guid? OperationalPerson(Tenant tenant, string userId)
    {
        return tenant.Memberships.Single(x => x.UserId == userId).PersonId;
    }

    public static string Role(Tenant tenant, string userId)
    {
        return tenant.Memberships.Single(x => x.UserId == userId).SecurityRole;
    }

    public static void RequireGrower(Tenant tenant, string userId)
    {
        if (Role(tenant, userId) != TenantSecurityRoles.Grower)
        {
            throw new ForbiddenAccessException();
        }
    }

    public static PayrollPeriod RequirePeriod(PayrollPeriod? period, Guid id)
    {
        return period ?? throw new NotFoundException(id.ToString(), "Payroll period");
    }

    public static WorkerAdvance RequireAdvance(WorkerAdvance? advance, Guid id)
    {
        return advance ?? throw new NotFoundException(id.ToString(), "Worker advance");
    }

    public static PayrollRun RequireRun(PayrollRun? run, Guid id)
    {
        return run ?? throw new NotFoundException(id.ToString(), "Payroll run");
    }

    public static void Domain(Action action, string property)
    {
        try { action(); }
        catch (InvalidOperationException exception) when (exception.Message.Contains("changed after it was loaded",
                                                              StringComparison.Ordinal))
        {
            throw new ConflictException(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationException([new ValidationFailure(property, exception.Message)]);
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException([new ValidationFailure(property, exception.Message)]);
        }
    }

    public static PayrollPeriodDto Period(PayrollPeriod period)
    {
        return new PayrollPeriodDto(period.Id, period.Year, period.Month, period.StartDate, period.EndDate,
            period.DisplayName,
            period.Status.ToString(), period.ClosedAt, period.ClosedByPayrollRunId, period.Version);
    }

    public static async Task<WorkerAdvanceDto> AdvanceAsync(IPayrollRepository payroll, WorkerAdvance advance,
        IReadOnlyDictionary<Guid, string> names, CancellationToken cancellationToken)
    {
        IReadOnlyList<AdvanceApproval> approvals =
            await payroll.GetApprovalsAsync(advance.TenantId, advance.FarmId, advance.Id, cancellationToken);
        AdvanceIssue? issue =
            await payroll.GetIssueAsync(advance.TenantId, advance.FarmId, advance.Id, cancellationToken);
        return Advance(advance, names, approvals, issue);
    }

    public static WorkerAdvanceDto Advance(WorkerAdvance advance, IReadOnlyDictionary<Guid, string> names,
        IReadOnlyList<AdvanceApproval>? approvals = null, AdvanceIssue? issue = null)
    {
        return new WorkerAdvanceDto(advance.Id, advance.WorkerProfileId,
            names.GetValueOrDefault(advance.WorkerProfileId, "Worker"),
            advance.RequestedAmountUsd, advance.ApprovedAmountUsd, advance.Reason, advance.RequestedEventDate,
            advance.RequestedAt, advance.RecoveryStartPayrollPeriodId, advance.InstallmentCount,
            advance.Status.ToString(), advance.Version, issue?.AmountUsd ?? 0m,
            advance.Installments.OrderBy(x => x.Sequence)
                .Select(x => new AdvanceInstallmentDto(x.Sequence, x.PayrollPeriodId, x.AmountUsd)).ToArray(),
            (approvals ?? []).Select(x => new AdvanceApprovalDto(x.AdvanceVersion, x.Approved, x.DecidedAt, x.Reason))
            .ToArray(),
            issue is null
                ? null
                : new AdvanceIssueDto(issue.PaymentMethod.ToString(), issue.AmountUsd, issue.IssuedAt,
                    issue.PayingPersonId, issue.ReceivingWorkerId, issue.WorkerAcknowledged, issue.Provider,
                    issue.MaskedRecipientNumber, issue.ExternalReference, issue.TransactionStatus));
    }
}
