namespace Cane360.Application.Finance;

public sealed record CostSourceDto(Guid Id, string Category, decimal AmountUsd, string SourceType,
    Guid SourceId, Guid? ActivityId, Guid FieldId, Guid CropCycleId, Guid? ReversalOfId,
    string SourceDescription, Guid? PayrollRunId, Guid? PayrollCalculationId,
    int? PayrollCalculationVersion, Guid? PayrollWorkerLineId, Guid? WorkerProfileId,
    Guid? WorkRecordId, Guid? OperationalTransactionId, Guid? TransactionAllocationId);
