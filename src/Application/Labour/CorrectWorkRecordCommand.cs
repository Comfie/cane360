using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

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
