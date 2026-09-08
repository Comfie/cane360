namespace Cane360.Application.Finance;

public sealed record OperationalTransactionDto(Guid Id, string Type, string Category,
    string EventDate, string PayeeOrPayer, decimal AmountUsd, string? SourceReference,
    string? Notes, string Status, long Version, DateTimeOffset CreatedAt, DateTimeOffset? PostedAt,
    bool IsClosedCycleCorrection, string? ClosedCycleCorrectionReason,
    Guid? ReversalOfOperationalTransactionId, string? ReversalReason,
    IReadOnlyCollection<TransactionAllocationDto> Allocations);
