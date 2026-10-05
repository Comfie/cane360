using Cane360.Domain.Labour;
using FluentValidation.Results;
using ValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Payroll;

public sealed class GetPayrollPreflightQueryHandler(
    IFarmSetupRepository farms,
    ILabourRepository labour,
    IPayrollRepository payroll,
    IUser user) : IRequestHandler<GetPayrollPreflightQuery, PayrollPreflightDto>
{
    public async Task<PayrollPreflightDto> Handle(GetPayrollPreflightQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.Page),
                    "Page must be positive and page size must be between 1 and 100.")
            ]);
        }

        (Tenant tenant, Farm farm, _) = await PayrollAccess.ContextAsync(farms, user, false,
            cancellationToken);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, request.PayrollPeriodId, false, cancellationToken),
            request.PayrollPeriodId);
        Dictionary<Guid, WorkerProfile> workers =
            (await labour.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(worker =>
                worker.Id);
        WorkRecord[] records =
            (await labour.GetWorkRecordsAsync(tenant.Id, farm.Id, null, request.WorkerId, null, false,
                cancellationToken))
            .Where(record => record.WorkDate >= period.StartDate && record.WorkDate <= period.EndDate).ToArray();
        HashSet<Guid> duplicateIds = records.Where(IsActive).GroupBy(record => record.PayBasis == PayBasis.Monthly
                ? $"monthly:{record.WorkerProfileId:N}:{record.WorkDate:yyyyMMdd}"
                : $"{record.WorkerProfileId:N}:{record.WorkDate:yyyyMMdd}:{string.Join(',', record.Activities.Select(activity => activity.ActivityId).Order())}")
            .Where(group => group.Count() > 1).SelectMany(group => group.Select(record => record.Id)).ToHashSet();
        List<PreflightEvidenceDto> result = new();

        foreach (WorkRecord record in records)
        {
            Attendance? attendance = await labour.GetAttendanceAsync(tenant.Id, farm.Id, record.WorkerProfileId,
                record.WorkDate, false, cancellationToken);
            workers.TryGetValue(record.WorkerProfileId, out WorkerProfile? worker);
            Field? field = farm.Fields.SingleOrDefault(candidate => candidate.Id == record.FieldId);
            Activity?[] activityDetails = record.Activities.Select(link =>
                farm.Fields.SelectMany(candidate => candidate.CropCycles).SelectMany(cycle => cycle.Activities)
                    .SingleOrDefault(activity => activity.Id == link.ActivityId)).ToArray();
            bool crossScope = record.TenantId != tenant.Id || record.FarmId != farm.Id ||
                              (attendance is not null &&
                               (attendance.TenantId != tenant.Id || attendance.FarmId != farm.Id)) || worker is null ||
                              worker.TenantId != tenant.Id || worker.FarmId != farm.Id;
            IReadOnlyList<PayrollPreflightBlocker> blockers = PayrollPreflightAssessment.Assess(
                new PayrollPreflightAssessmentInput(
                    record.WorkDate < period.StartDate || record.WorkDate > period.EndDate,
                    attendance is null || attendance.Status != AttendanceStatus.Present,
                    attendance?.FieldId is null,
                    attendance?.FieldId is not null && attendance.FieldId != record.FieldId,
                    record.Verification is null,
                    record.Verification?.ManagerConfirmedAt is null,
                    record.Status == WorkRecordStatus.Superseded,
                    record.Status == WorkRecordStatus.Cancelled,
                    record.AppliedRateUsd <= 0 || record.WorkerRateId == Guid.Empty,
                    record.PayBasis == PayBasis.Monthly &&
                    record.AppliedRateUsd < DateTime.DaysInMonth(period.Year, period.Month) * .01m,
                    duplicateIds.Contains(record.Id) ||
                    (record.Scopes.Any(scope => scope.SupersededAt is not null) && IsActive(record)),
                    crossScope,
                    !crossScope && worker!.Status != RecordStatus.Active,
                    field is null || activityDetails.Any(activity =>
                        activity is null || activity.TenantId != tenant.Id || activity.FarmId != farm.Id ||
                        activity.FieldId != record.FieldId || activity.IsTerminal)));
            string[] codes = blockers.Select(blocker => blocker.Code).ToArray();
            string[] explanations = blockers.Select(blocker => blocker.Explanation).ToArray();
            CropCycle?[] cropCycles = activityDetails.Where(activity => activity is not null)
                .Select(activity => farm.Fields.SelectMany(candidate => candidate.CropCycles)
                    .SingleOrDefault(cycle => cycle.Id == activity!.CropCycleId)).Where(cycle => cycle is not null)
                .DistinctBy(cycle => cycle!.Id).ToArray();
            string cropLabel = cropCycles.Length == 0
                ? "Unknown crop cycle"
                : string.Join(", ", cropCycles.Select(cycle => $"{cycle!.Variety} · {cycle.StartDate.Year}"));
            List<PreflightSourceLinkDto> sourceChain = new();
            if (attendance is not null)
            {
                sourceChain.Add(new PreflightSourceLinkDto("Attendance", attendance.Id,
                    $"{attendance.Status} attendance"));
            }

            sourceChain.Add(new PreflightSourceLinkDto("WorkEvidence", record.Id, $"{record.PayBasis} work evidence"));
            if (record.Verification is not null)
            {
                sourceChain.Add(new PreflightSourceLinkDto("Verification", record.Verification.Id,
                    record.Verification.ManagerConfirmedAt is null
                        ? "Supervisor attested"
                        : "Supervisor attested · manager confirmed"));
            }

            foreach (Activity? activity in activityDetails.Where(activity => activity is not null))
            {
                sourceChain.Add(new PreflightSourceLinkDto("Activity", activity!.Id, activity.ActivityTypeName));
            }

            if (record.CorrectsWorkRecordId is Guid correctionId)
            {
                sourceChain.Add(new PreflightSourceLinkDto("Correction", correctionId, "Corrected evidence"));
            }

            result.Add(new PreflightEvidenceDto(record.WorkerProfileId,
                worker is null ? "Worker" : farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName,
                record.Id, "WorkRecord", record.WorkDate, record.FieldId, field?.Name ?? "Unknown field", cropLabel,
                record.Activities.Select(activity => activity.ActivityId).ToArray(),
                activityDetails.Where(activity => activity is not null).Select(activity => activity!.ActivityTypeName)
                    .ToArray(), record.Quantity,
                record.Quantity is null
                    ? $"{record.PayBasis} attendance basis"
                    : $"{record.Quantity:0.####} {record.PayBasis}", record.AppliedRateUsd, record.PayBasis.ToString(),
                codes.Length == 0, codes, explanations, sourceChain));
        }

        IEnumerable<PreflightEvidenceDto> filtered = result;
        if (request.Eligible.HasValue)
        {
            filtered = filtered.Where(item => item.Eligible == request.Eligible.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EvidenceType))
        {
            filtered = filtered.Where(item =>
                item.EvidenceType.Equals(request.EvidenceType, StringComparison.OrdinalIgnoreCase));
        }

        PreflightEvidenceDto[] complete = filtered.OrderBy(item => item.WorkerName).ThenBy(item => item.EventDate)
            .ThenBy(item => item.EvidenceId).ToArray();
        PreflightWorkerTotalDto[] workerTotals = complete.GroupBy(item => new { item.WorkerId, item.WorkerName })
            .Select(group => new PreflightWorkerTotalDto(group.Key.WorkerId, group.Key.WorkerName,
                group.Count(item => item.Eligible), group.Count(item => !item.Eligible)))
            .OrderBy(item => item.WorkerName).ToArray();
        PreflightEvidenceTypeTotalDto[] evidenceTotals = complete.GroupBy(item => item.EvidenceType)
            .Select(group => new PreflightEvidenceTypeTotalDto(group.Key, group.Count(item => item.Eligible),
                group.Count(item => !item.Eligible))).OrderBy(item => item.EvidenceType).ToArray();
        PreflightEvidenceDto[] page = complete.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToArray();
        return new PayrollPreflightDto(period.Id, page, complete.Count(item => item.Eligible),
            complete.Count(item => !item.Eligible), workerTotals.Count(item => item.EligibleCount > 0),
            workerTotals.Count(item => item.BlockedCount > 0), complete.Length, request.Page, request.PageSize,
            workerTotals, evidenceTotals);
    }

    private static bool IsActive(WorkRecord record)
    {
        return record.Status is not (WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded);
    }
}
