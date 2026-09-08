namespace Cane360.Application.Finance;

public sealed record CostSourceChain(Guid PostingId, Guid? PayrollRunId,
    Guid? PayrollCalculationId, int? PayrollCalculationVersion, Guid? PayrollWorkerLineId,
    Guid? WorkerProfileId, Guid? WorkRecordId, Guid? OperationalTransactionId,
    Guid? TransactionAllocationId);
