namespace Cane360.Web.Models.Labour;

public sealed record CreateWorkRecordRequest(
    Guid WorkerId,
    string WorkDate,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeRequest? Scope,
    string? LateEntryReason);
