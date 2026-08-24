using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record CreateInputApplicationCommand(Guid ActivityId, DateTimeOffset AppliedAt,
    ApplicationCoverageBasis CoverageBasis, decimal VerifiedCoverage,
    IReadOnlyList<CreateInputApplicationLineCommand> Lines) : IRequest<Guid>;
