namespace Cane360.Web.Models.Labour;

public sealed record CreateWorkerRequest(
    Guid? PersonId,
    string? DisplayName,
    string? Phone,
    string EmploymentType,
    string ActiveFrom,
    string NationalId);

public sealed record ArchiveWorkerRequest(string ActiveTo, long ExpectedVersion);
public sealed record RevealNationalIdRequest(string Reason);

public sealed record CreateWorkerRateRequest(
    string Basis,
    Guid? ActivityTypeId,
    decimal RateUsd,
    string EffectiveFrom,
    string? EffectiveTo);
public sealed record EndWorkerRateRequest(string EffectiveTo, long ExpectedVersion);

public sealed record AttendanceEntryRequest(Guid WorkerId, string Status, Guid? FieldId, long? ExpectedVersion);
public sealed record RecordAttendanceRequest(string WorkDate, string? LateEntryReason, IReadOnlyList<AttendanceEntryRequest> Entries);

public sealed record WorkScopeRequest(string Type, int? StartLine, int? EndLine, string? SectionName);
public sealed record CreateWorkRecordRequest(
    Guid WorkerId,
    string WorkDate,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeRequest? Scope,
    string? LateEntryReason);
public sealed record VerifyWorkRecordRequest(Guid SupervisorPersonId, long ExpectedVersion);
public sealed record ConfirmWorkRecordRequest(long ExpectedVersion);
public sealed record CorrectWorkRecordRequest(
    long ExpectedVersion,
    string CorrectionReason,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeRequest? Scope,
    string? LateEntryReason);
