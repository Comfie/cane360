namespace Cane360.Application.Labour;

public sealed record CorrectWorkRecordCommand(
    Guid WorkRecordId,
    long ExpectedVersion,
    string CorrectionReason,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeCommand? Scope,
    string? LateEntryReason) : IRequest<WorkRecordDto>;
