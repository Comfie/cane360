namespace Cane360.Web.Models.Labour;

public sealed record CorrectWorkRecordRequest(
    long ExpectedVersion,
    string CorrectionReason,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeRequest? Scope,
    string? LateEntryReason);
