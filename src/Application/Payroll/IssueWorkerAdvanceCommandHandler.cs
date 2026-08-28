using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class IssueWorkerAdvanceCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user) : IRequestHandler<IssueWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(IssueWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var priorIssue = await payroll.GetIssueByKeyAsync(tenant.Id, farm.Id, request.IdempotencyKey, cancellationToken);
        if (priorIssue is not null)
        {
            if (priorIssue.WorkerAdvanceId != request.AdvanceId || priorIssue.AmountUsd != request.AmountUsd || priorIssue.PaymentMethod != request.PaymentMethod) throw new ConflictException("This issuance idempotency key is already bound to different issue evidence.");
            var prior = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, priorIssue.WorkerAdvanceId, false, cancellationToken), priorIssue.WorkerAdvanceId); var pworker = await labour.GetWorkerAsync(tenant.Id, farm.Id, prior.WorkerProfileId, false, cancellationToken); return await PayrollAccess.AdvanceAsync(payroll, prior, new Dictionary<Guid, string> { [prior.WorkerProfileId] = pworker is null ? "Worker" : farm.Persons.Single(x => x.Id == pworker.PersonId).DisplayName }, cancellationToken);
        }
        var advance = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, true, cancellationToken), request.AdvanceId); AdvanceIssue? issue = null;
        if (request.PaymentMethod == AdvancePaymentMethod.Cash && (request.PayingPersonId is null || farm.Persons.All(person => person.Id != request.PayingPersonId || person.Status != RecordStatus.Active))) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.PayingPersonId), "Select an active paying person from this farm.")]);
        var issuedAt = request.IssuedAt.ToUniversalTime();
        PayrollAccess.Domain(() => issue = request.PaymentMethod == AdvancePaymentMethod.Cash ? AdvanceIssue.Cash(advance.Id, tenant.Id, farm.Id, request.AmountUsd, issuedAt, userId, request.PayingPersonId ?? Guid.Empty, advance.WorkerProfileId, request.WorkerAcknowledged == true, request.IdempotencyKey) : AdvanceIssue.MobileMoney(advance.Id, tenant.Id, farm.Id, request.AmountUsd, issuedAt, userId, request.Provider!, MaskRecipient(request.RecipientNumber!), request.ExternalReference!, request.TransactionStatus!, request.IdempotencyKey), nameof(request.PaymentMethod)); PayrollAccess.Domain(() => advance.Issue(request.AmountUsd, request.ExpectedVersion), nameof(request.ExpectedVersion)); payroll.Add(issue!); PayrollAudit.Issue(payroll, tenant, farm, user, advance, issue!, issuedAt, "Operational cash or mobile-money issue evidence recorded; no external payment was executed."); await payroll.SaveChangesAsync(cancellationToken); var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken); return await PayrollAccess.AdvanceAsync(payroll, advance, new Dictionary<Guid, string> { [advance.WorkerProfileId] = worker is null ? "Worker" : farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName }, cancellationToken);
    }

    private static string MaskRecipient(string? recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient)) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(IssueWorkerAdvanceCommand.RecipientNumber), "A valid recipient number is required.")]);
        var digits = new string(recipient.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(IssueWorkerAdvanceCommand.RecipientNumber), "A valid recipient number is required.")]);
        return $"•••• {digits[^4..]}";
    }
}
