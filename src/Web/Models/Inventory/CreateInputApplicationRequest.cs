using Cane360.Domain.Inventory;

namespace Cane360.Web.Models.Inventory;

public sealed record CreateInputApplicationRequest(Guid ActivityId, DateTimeOffset AppliedAt,
    ApplicationCoverageBasis CoverageBasis, decimal VerifiedCoverage,
    IReadOnlyList<CreateInputApplicationLineRequest> Lines);
