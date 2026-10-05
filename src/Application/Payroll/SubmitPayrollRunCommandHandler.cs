using System.Text.Json;

namespace Cane360.Application.Payroll;

public sealed class SubmitPayrollRunCommandHandler(
    IFarmSetupRepository farms,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock) : IRequestHandler<SubmitPayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(SubmitPayrollRunCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) =
            await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        PayrollRun run = PayrollAccess.RequireRun(
            await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, true, cancellationToken),
            request.PayrollRunId);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, false, cancellationToken),
            run.PayrollPeriodId);
        PayrollCalculation calculation =
            await payroll.GetCalculationAsync(tenant.Id, farm.Id, run.Id, request.CalculationVersion,
                cancellationToken) ??
            throw new NotFoundException(request.CalculationVersion.ToString(), "Payroll calculation");
        string[] blockers = JsonSerializer.Deserialize<string[]>(calculation.BlockerSnapshot) ?? [];
        if (period.Status != PayrollPeriodStatus.Open || blockers.Length != 0 || calculation.EvidenceCount == 0)
        {
            throw new ConflictException(
                $"PayrollCalculationIncomplete: submission is blocked by {string.Join(", ", blockers.DefaultIfEmpty(PayrollPreflightBlockerCodes.PayrollCalculationIncomplete))}.");
        }

        DateTimeOffset now = clock.GetUtcNow();
        PayrollAccess.Domain(() => run.Submit(request.CalculationVersion, now, userId, request.ExpectedVersion),
            nameof(request.ExpectedVersion));
        PayrollAudit.Run(payroll, tenant, farm, user, run, "PayrollSubmitted", now, null,
            $"FarmManager submitted immutable calculation version {request.CalculationVersion} for Grower approval.");
        await payroll.SaveChangesAsync(cancellationToken);
        return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken);
    }
}
