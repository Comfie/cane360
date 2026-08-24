using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

internal static class LabourMapper
{
    public static WorkerListItemDto Worker(Farm farm, WorkerProfile worker)
    {
        var person = farm.Persons.Single(candidate => candidate.Id == worker.PersonId);
        return new WorkerListItemDto(worker.Id, person.Id, person.DisplayName, person.Phone,
            worker.EmploymentType.ToString(), worker.ActiveFrom, worker.ActiveTo,
            worker.Status.ToString(), worker.NationalIdMask, worker.Version);
    }

    public static WorkerRateDto Rate(Tenant tenant, WorkerRate rate) => new(
        rate.Id, rate.Basis.ToString(), rate.ActivityTypeId,
        rate.ActivityTypeId.HasValue ? tenant.ActivityTypes.SingleOrDefault(type => type.Id == rate.ActivityTypeId)?.Name : null,
        rate.RateUsd, rate.EffectiveFrom, rate.EffectiveTo, rate.Version);

    public static WorkRecordDto Work(Tenant tenant, Farm farm, IReadOnlyDictionary<Guid, WorkerProfile> workers, WorkRecord record)
    {
        var worker = workers[record.WorkerProfileId];
        var person = farm.Persons.Single(candidate => candidate.Id == worker.PersonId);
        var field = farm.Fields.Single(candidate => candidate.Id == record.FieldId);
        var activities = farm.Fields.SelectMany(item => item.CropCycles).SelectMany(cycle => cycle.Activities)
            .Where(activity => record.Activities.Any(link => link.ActivityId == activity.Id)).ToArray();
        WorkVerificationDto? verification = null;
        if (record.Verification is not null)
        {
            var supervisor = farm.Persons.Single(candidate => candidate.Id == record.Verification.SupervisorPersonId);
            verification = new WorkVerificationDto(
                supervisor.Id, supervisor.DisplayName, record.Verification.SupervisorVerifiedAt,
                $"Entered by manager; verification provided by {supervisor.DisplayName}.",
                record.Verification.ManagerConfirmedAt, record.Verification.ManagerConfirmedByUserId);
        }

        return new WorkRecordDto(record.Id, record.WorkerProfileId, person.DisplayName, record.AttendanceId,
            field.Id, field.Name, record.WorkDate, record.PayBasis.ToString(), record.AppliedRateUsd,
            record.Quantity, record.CalculatedAmountUsd, record.Status.ToString(),
            record.Activities.Select(link => link.ActivityId).ToArray(), activities.Select(activity => activity.ActivityTypeName).ToArray(),
            record.Scopes.Select(scope => new WorkScopeDto(scope.ScopeType.ToString(), scope.ActivityId,
                scope.FieldLineProfileId, scope.StartLine, scope.EndLine, scope.SectionName)).ToArray(),
            verification, record.EnteredAt, record.EntryDelayDays, record.CorrectsWorkRecordId, record.Version);
    }
}
