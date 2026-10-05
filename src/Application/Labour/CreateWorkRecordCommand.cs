namespace Cane360.Application.Labour;

public sealed record CreateWorkRecordCommand(
    Guid WorkerId,
    DateOnly WorkDate,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeCommand? Scope,
    string? LateEntryReason) : IRequest<WorkRecordDto>;
