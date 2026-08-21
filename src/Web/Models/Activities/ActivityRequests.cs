namespace Cane360.Web.Models.Activities;

public sealed record CreateActivityTypeRequest(
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis);

public sealed record VersionedRequest(long ExpectedVersion);

public sealed record CreatePersonRequest(
    string DisplayName,
    string? Phone,
    DateOnly ActiveFrom,
    IReadOnlyList<string> Roles,
    bool IsPrimaryManager);

public sealed record DeactivatePersonRequest(long ExpectedVersion, DateOnly ActiveTo);
public sealed record EndPersonRoleRequest(long ExpectedVersion, DateOnly EffectiveTo);

public sealed record ReplaceFieldLineProfileRequest(
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    DateOnly EffectiveFrom,
    long? ExpectedVersion);

public sealed record CreateActivityRequest(
    Guid FieldId,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string Kind,
    DateOnly? PlannedDate,
    Guid SupervisorPersonId);

public sealed record RecordActualWorkRequest(
    long ExpectedVersion,
    string ActualAt,
    decimal? ActualQuantity,
    string? LateEntryReason);

public sealed record TransitionActivityRequest(long ExpectedVersion, string? Reason);

public sealed record AddSourceReferenceRequest(
    long ExpectedVersion,
    string SourceSheetReference,
    DateOnly CapturedDate);
