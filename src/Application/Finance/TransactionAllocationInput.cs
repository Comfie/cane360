namespace Cane360.Application.Finance;

public sealed record TransactionAllocationInput(Guid? CropCycleId, Guid? FieldId, string Category,
    decimal AmountUsd, string AllocationType);
