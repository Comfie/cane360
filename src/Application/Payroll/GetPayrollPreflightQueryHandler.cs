using Cane360.Domain.Labour;

namespace Cane360.Application.Payroll;

public sealed class GetPayrollPreflightQueryHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user) : IRequestHandler<GetPayrollPreflightQuery, PayrollPreflightDto>
{
    public async Task<PayrollPreflightDto> Handle(GetPayrollPreflightQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.Page), "Page must be positive and page size must be between 1 and 100.")]);
        var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, request.PayrollPeriodId, false, cancellationToken), request.PayrollPeriodId);
        var workers = (await labour.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(worker => worker.Id);
        var records = await labour.GetWorkRecordsAsync(tenant.Id, farm.Id, null, request.WorkerId, null, false, cancellationToken);
        var duplicateIds = records.Where(IsActive).GroupBy(record => $"{record.WorkerProfileId:N}:{record.WorkDate:yyyyMMdd}:{string.Join(',', record.Activities.Select(activity => activity.ActivityId).Order())}").Where(group => group.Count() > 1).SelectMany(group => group.Select(record => record.Id)).ToHashSet();
        var result = new List<PreflightEvidenceDto>();

        foreach (var record in records)
        {
            var attendance = await labour.GetAttendanceAsync(tenant.Id, farm.Id, record.WorkerProfileId, record.WorkDate, false, cancellationToken);
            workers.TryGetValue(record.WorkerProfileId, out var worker);
            var field = farm.Fields.SingleOrDefault(candidate => candidate.Id == record.FieldId);
            var activityDetails = record.Activities.Select(link => farm.Fields.SelectMany(candidate => candidate.CropCycles).SelectMany(cycle => cycle.Activities).SingleOrDefault(activity => activity.Id == link.ActivityId)).ToArray();
            var crossScope = record.TenantId != tenant.Id || record.FarmId != farm.Id || attendance is not null && (attendance.TenantId != tenant.Id || attendance.FarmId != farm.Id) || worker is null || worker.TenantId != tenant.Id || worker.FarmId != farm.Id;
            var blockers = PayrollPreflightAssessment.Assess(new PayrollPreflightAssessmentInput(
                record.WorkDate < period.StartDate || record.WorkDate > period.EndDate,
                attendance is null || attendance.Status != AttendanceStatus.Present,
                attendance?.FieldId is null,
                attendance?.FieldId is not null && attendance.FieldId != record.FieldId,
                record.Verification is null,
                record.Verification?.ManagerConfirmedAt is null,
                record.Status == WorkRecordStatus.Superseded,
                record.Status == WorkRecordStatus.Cancelled,
                record.AppliedRateUsd <= 0 || record.WorkerRateId == Guid.Empty,
                record.PayBasis == PayBasis.Monthly,
                duplicateIds.Contains(record.Id) || record.Scopes.Any(scope => scope.SupersededAt is not null) && IsActive(record),
                crossScope,
                !crossScope && worker!.Status != RecordStatus.Active,
                field is null || activityDetails.Any(activity => activity is null || activity.TenantId != tenant.Id || activity.FarmId != farm.Id || activity.FieldId != record.FieldId || activity.IsTerminal)));
            var codes = blockers.Select(blocker => blocker.Code).ToArray();
            var explanations = blockers.Select(blocker => blocker.Explanation).ToArray();
            var cropCycles = activityDetails.Where(activity => activity is not null).Select(activity => farm.Fields.SelectMany(candidate => candidate.CropCycles).SingleOrDefault(cycle => cycle.Id == activity!.CropCycleId)).Where(cycle => cycle is not null).DistinctBy(cycle => cycle!.Id).ToArray();
            var cropLabel = cropCycles.Length == 0 ? "Unknown crop cycle" : string.Join(", ", cropCycles.Select(cycle => $"{cycle!.Variety} · {cycle.StartDate.Year}"));
            var sourceChain = new List<PreflightSourceLinkDto>();
            if (attendance is not null) sourceChain.Add(new PreflightSourceLinkDto("Attendance", attendance.Id, $"{attendance.Status} attendance"));
            sourceChain.Add(new PreflightSourceLinkDto("WorkEvidence", record.Id, $"{record.PayBasis} work evidence"));
            if (record.Verification is not null) sourceChain.Add(new PreflightSourceLinkDto("Verification", record.Verification.Id, record.Verification.ManagerConfirmedAt is null ? "Supervisor attested" : "Supervisor attested · manager confirmed"));
            foreach (var activity in activityDetails.Where(activity => activity is not null)) sourceChain.Add(new PreflightSourceLinkDto("Activity", activity!.Id, activity.ActivityTypeName));
            if (record.CorrectsWorkRecordId is Guid correctionId) sourceChain.Add(new PreflightSourceLinkDto("Correction", correctionId, "Corrected evidence"));
            result.Add(new PreflightEvidenceDto(record.WorkerProfileId, worker is null ? "Worker" : farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName, record.Id, "WorkRecord", record.WorkDate, record.FieldId, field?.Name ?? "Unknown field", cropLabel, record.Activities.Select(activity => activity.ActivityId).ToArray(), activityDetails.Where(activity => activity is not null).Select(activity => activity!.ActivityTypeName).ToArray(), record.Quantity, record.Quantity is null ? $"{record.PayBasis} attendance basis" : $"{record.Quantity:0.####} {record.PayBasis}", record.AppliedRateUsd, record.PayBasis.ToString(), codes.Length == 0, codes, explanations, sourceChain));
        }

        IEnumerable<PreflightEvidenceDto> filtered = result;
        if (request.Eligible.HasValue) filtered = filtered.Where(item => item.Eligible == request.Eligible.Value);
        if (!string.IsNullOrWhiteSpace(request.EvidenceType)) filtered = filtered.Where(item => item.EvidenceType.Equals(request.EvidenceType, StringComparison.OrdinalIgnoreCase));
        var complete = filtered.OrderBy(item => item.WorkerName).ThenBy(item => item.EventDate).ThenBy(item => item.EvidenceId).ToArray();
        var workerTotals = complete.GroupBy(item => new { item.WorkerId, item.WorkerName }).Select(group => new PreflightWorkerTotalDto(group.Key.WorkerId, group.Key.WorkerName, group.Count(item => item.Eligible), group.Count(item => !item.Eligible))).OrderBy(item => item.WorkerName).ToArray();
        var evidenceTotals = complete.GroupBy(item => item.EvidenceType).Select(group => new PreflightEvidenceTypeTotalDto(group.Key, group.Count(item => item.Eligible), group.Count(item => !item.Eligible))).OrderBy(item => item.EvidenceType).ToArray();
        var page = complete.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new PayrollPreflightDto(period.Id, "Monthly work evidence remains uncalculated. Phase 6B must define and approve a proration rule before it can be processed.", page, complete.Count(item => item.Eligible), complete.Count(item => !item.Eligible), workerTotals.Count(item => item.EligibleCount > 0), workerTotals.Count(item => item.BlockedCount > 0), complete.Length, request.Page, request.PageSize, workerTotals, evidenceTotals);
    }

    private static bool IsActive(WorkRecord record) => record.Status is not (WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded);
}
