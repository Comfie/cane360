namespace Cane360.Application.Finance;

public sealed record SetTransactionAllocationsInput(long ExpectedVersion,
    IReadOnlyCollection<TransactionAllocationInput> Allocations);
