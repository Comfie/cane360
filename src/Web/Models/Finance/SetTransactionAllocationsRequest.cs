namespace Cane360.Web.Models.Finance;

public sealed record SetTransactionAllocationsRequest(long ExpectedVersion,
    IReadOnlyCollection<TransactionAllocationRequest> Allocations);
