using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record WorkerListItemDto(
    Guid Id,
    Guid PersonId,
    string DisplayName,
    string? Phone,
    string EmploymentType,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    string Status,
    string NationalIdMask,
    long Version);

public sealed record WorkerRateDto(
    Guid Id,
    string Basis,
    Guid? ActivityTypeId,
    string? ActivityTypeName,
    decimal RateUsd,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    long Version);

public sealed record WorkerDetailsDto(WorkerListItemDto Worker, IReadOnlyList<WorkerRateDto> Rates);

public sealed record RevealedNationalIdDto(Guid WorkerId, string NationalId);

public sealed record AttendanceRowDto(
    Guid WorkerId,
    string WorkerName,
    string EmploymentType,
    Guid? AttendanceId,
    DateOnly WorkDate,
    string? Status,
    Guid? FieldId,
    string? FieldName,
    int EntryDelayDays,
    long? Version);

public sealed record AttendanceRegisterDto(
    DateOnly WorkDate,
    IReadOnlyList<AttendanceRowDto> Rows,
    IReadOnlyList<LabourFieldDto> Fields);

public sealed record LabourFieldDto(Guid Id, string Code, string Name);
public sealed record LabourActivityDto(Guid Id, Guid ActivityTypeId, string Name, Guid FieldId, DateOnly WorkDate, string QuantityBasis, string Status);
public sealed record LabourPersonDto(Guid Id, string DisplayName);

public sealed record WorkScopeDto(
    string Type,
    Guid ActivityId,
    Guid? FieldLineProfileId,
    int? StartLine,
    int? EndLine,
    string? SectionName);

public sealed record WorkVerificationDto(
    Guid SupervisorPersonId,
    string SupervisorName,
    DateTimeOffset SupervisorVerifiedAt,
    string Attestation,
    DateTimeOffset? ManagerConfirmedAt,
    string? ManagerConfirmedByUserId);

public sealed record WorkRecordDto(
    Guid Id,
    Guid WorkerId,
    string WorkerName,
    Guid AttendanceId,
    Guid FieldId,
    string FieldName,
    DateOnly WorkDate,
    string PayBasis,
    decimal AppliedRateUsd,
    decimal? Quantity,
    decimal? CalculatedAmountUsd,
    string Status,
    IReadOnlyList<Guid> ActivityIds,
    IReadOnlyList<string> ActivityNames,
    IReadOnlyList<WorkScopeDto> Scopes,
    WorkVerificationDto? Verification,
    DateTimeOffset EnteredAt,
    int EntryDelayDays,
    Guid? CorrectsWorkRecordId,
    long Version);

public sealed record LabourReferenceDataDto(
    IReadOnlyList<LabourFieldDto> Fields,
    IReadOnlyList<LabourActivityDto> Activities,
    IReadOnlyList<LabourPersonDto> Supervisors);

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
