using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cane360.Domain.Labour;

namespace Cane360.Application.Payroll;

internal static class PayrollCalculationBuilder
{
    public static async Task<PayrollCalculation> BuildAsync(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, Tenant tenant, Farm farm, PayrollPeriod period, PayrollRun run, int version, DateTimeOffset at, string userId, Guid? personId, CancellationToken cancellationToken)
    {
        var calculationId = Guid.NewGuid();
        var records = (await labour.GetWorkRecordsAsync(tenant.Id, farm.Id, null, null, null, false, cancellationToken)).Where(x => x.WorkDate >= period.StartDate && x.WorkDate <= period.EndDate).OrderBy(x => x.WorkDate).ThenBy(x => x.Id).ToArray();
        var workers = (await labour.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(x => x.Id);
        var consumed = await payroll.GetConsumedEvidenceIdsAsync(tenant.Id, farm.Id, cancellationToken);
        var duplicateIds = records.Where(IsActive).GroupBy(record => $"{record.WorkerProfileId:N}:{record.WorkDate:yyyyMMdd}:{string.Join(',', record.Activities.Select(activity => activity.ActivityId).Order())}").Where(group => group.Count() > 1).SelectMany(group => group.Select(record => record.Id)).ToHashSet();
        var blockers = new List<string>();
        var sourceTokens = new List<string> { $"period:{period.Id:N}:{period.Status}:{period.Version}" };
        if (period.Status != PayrollPeriodStatus.Open) blockers.Add(PayrollPreflightBlockerCodes.PayrollPeriodNotOpen);
        var earningGroups = new Dictionary<Guid, (Guid LineId, string Name, List<PayrollEarningLine> Lines)>();

        foreach (var record in records)
        {
            var attendance = await labour.GetAttendanceAsync(tenant.Id, farm.Id, record.WorkerProfileId, record.WorkDate, false, cancellationToken);
            workers.TryGetValue(record.WorkerProfileId, out var worker);
            var field = farm.Fields.SingleOrDefault(candidate => candidate.Id == record.FieldId);
            var activities = record.Activities.Select(link => farm.Fields.SelectMany(candidate => candidate.CropCycles).SelectMany(cycle => cycle.Activities).SingleOrDefault(activity => activity.Id == link.ActivityId)).ToArray();
            var crossScope = record.TenantId != tenant.Id || record.FarmId != farm.Id || attendance is not null && (attendance.TenantId != tenant.Id || attendance.FarmId != farm.Id) || worker is null || worker.TenantId != tenant.Id || worker.FarmId != farm.Id;
            var assessed = PayrollPreflightAssessment.Assess(new PayrollPreflightAssessmentInput(false, attendance is null || attendance.Status != AttendanceStatus.Present, attendance?.FieldId is null, attendance?.FieldId is not null && attendance.FieldId != record.FieldId, record.Verification is null, record.Verification?.ManagerConfirmedAt is null, record.Status == WorkRecordStatus.Superseded, record.Status == WorkRecordStatus.Cancelled, record.AppliedRateUsd <= 0 || record.WorkerRateId == Guid.Empty, record.PayBasis == PayBasis.Monthly, duplicateIds.Contains(record.Id) || record.Scopes.Any(scope => scope.SupersededAt is not null) && IsActive(record), crossScope, !crossScope && worker!.Status != RecordStatus.Active, field is null || activities.Any(activity => activity is null || activity.TenantId != tenant.Id || activity.FarmId != farm.Id || activity.FieldId != record.FieldId || activity.IsTerminal))).Select(x => x.Code).ToList();
            if (consumed.Contains(record.Id)) assessed.Add(PayrollPreflightBlockerCodes.EvidenceAlreadyConsumedByPayroll);
            blockers.AddRange(assessed);
            var currentRate = worker is null ? null : (await labour.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken)).SingleOrDefault(x => x.Id == record.WorkerRateId);
            var token = string.Join('|', record.Id, record.Version, record.Status, record.WorkDate.ToString("O", CultureInfo.InvariantCulture), record.AttendanceId, attendance?.Version, attendance?.Status, attendance?.FieldId, record.WorkerProfileId, worker?.Status, record.FieldId, record.PayBasis, record.Quantity, record.AppliedRateUsd, record.WorkerRateId, currentRate?.Version, currentRate?.RateUsd, record.Verification?.SupervisorVerifiedAt, record.Verification?.ManagerConfirmedAt, string.Join(',', record.Activities.Select(x => x.ActivityId).Order()), string.Join(',', assessed.Order()));
            sourceTokens.Add(token);
            if (assessed.Count != 0 || worker is null || attendance is null) continue;
            if (!earningGroups.TryGetValue(worker.Id, out var group)) group = (Guid.NewGuid(), farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName, []);
            var quantity = record.PayBasis == PayBasis.Daily ? 1m : record.Quantity!.Value;
            var unit = record.PayBasis switch { PayBasis.Daily => "day", PayBasis.Hectare => "hectare", PayBasis.StandardLine => "standard-line", _ => throw new InvalidOperationException() };
            group.Lines.Add(PayrollEarningLine.Create(group.LineId, calculationId, tenant.Id, farm.Id, worker.Id, record.Id, "WorkRecord", record.WorkDate, attendance.Id, attendance.Version, record.Verification!.SupervisorVerifiedAt, record.Verification.ManagerConfirmedAt!.Value, record.FieldId, JsonSerializer.Serialize(record.Activities.Select(x => x.ActivityId).Order()), quantity, unit, record.PayBasis.ToString(), record.AppliedRateUsd, record.WorkerRateId, currentRate?.Version ?? 0, Hash(token)));
            earningGroups[worker.Id] = group;
        }

        var allPeriods = (await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(x => x.Id);
        var advances = await payroll.GetAdvancesAsync(tenant.Id, farm.Id, false, cancellationToken);
        var recoveries = await payroll.GetRecoveriesAsync(tenant.Id, farm.Id, cancellationToken);
        var workerLines = new List<PayrollWorkerLine>();
        foreach (var pair in earningGroups.OrderBy(x => x.Value.Name).ThenBy(x => x.Key))
        {
            var gross = pair.Value.Lines.Sum(x => x.EarningAmountUsd); var deductions = new List<PayrollAdvanceDeduction>();
            var candidates = new List<(WorkerAdvance Advance, AdvanceInstallment Installment, PayrollPeriod DuePeriod, DateTimeOffset IssuedAt, decimal Outstanding)>();
            foreach (var advance in advances.Where(x => x.WorkerProfileId == pair.Key && x.Status == AdvanceStatus.Issued))
            {
                var issue = await payroll.GetIssueAsync(tenant.Id, farm.Id, advance.Id, cancellationToken);
                if (issue is null) continue;
                foreach (var installment in advance.Installments)
                {
                    if (!allPeriods.TryGetValue(installment.PayrollPeriodId, out var due) || (due.Year, due.Month).CompareTo((period.Year, period.Month)) > 0) continue;
                    var recovered = recoveries.Where(x => x.AdvanceInstallmentId == installment.Id).Sum(x => x.AmountUsd); var outstanding = installment.AmountUsd - recovered;
                    if (outstanding > 0) candidates.Add((advance, installment, due, issue.IssuedAt, outstanding));
                }
            }
            var orderedAllocations = AdvanceRecoveryAllocator.Allocate(gross, candidates.Select(x => new AdvanceRecoveryCandidate(x.Advance.Id, x.Installment.Id, x.DuePeriod.Id, x.DuePeriod.Year, x.DuePeriod.Month, x.IssuedAt, x.Installment.Sequence, x.Installment.AmountUsd, x.Outstanding)));
            foreach (var allocation in orderedAllocations)
            {
                var candidate = candidates.Single(x => x.Advance.Id == allocation.Candidate.WorkerAdvanceId && x.Installment.Id == allocation.Candidate.AdvanceInstallmentId);
                deductions.Add(PayrollAdvanceDeduction.Create(pair.Value.LineId, calculationId, tenant.Id, farm.Id, pair.Key, candidate.Advance.Id, candidate.Installment.Id, candidate.DuePeriod.Id, candidate.Installment.Sequence, candidate.Installment.AmountUsd, candidate.Outstanding, allocation.AmountUsd));
                sourceTokens.Add($"advance:{candidate.Advance.Id:N}:{candidate.Advance.Version}:{candidate.Advance.Status}:{candidate.IssuedAt:O}:{candidate.Installment.Id:N}:{candidate.Outstanding}:{allocation.AmountUsd}");
            }
            workerLines.Add(PayrollWorkerLine.Create(pair.Value.LineId, calculationId, tenant.Id, farm.Id, pair.Key, pair.Value.Name, pair.Value.Lines, deductions));
        }
        if (records.Length == 0 || workerLines.Count == 0) blockers.Add(PayrollPreflightBlockerCodes.PayrollCalculationIncomplete);
        return PayrollCalculation.Create(calculationId, run.Id, period.Id, tenant.Id, farm.Id, version, workerLines, blockers.Distinct().ToArray(), Hash(string.Join('\n', sourceTokens.Order())), at, userId, personId);
    }

    private static bool IsActive(WorkRecord record) => record.Status is not (WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
