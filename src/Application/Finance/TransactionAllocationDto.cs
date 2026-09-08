namespace Cane360.Application.Finance;

public sealed record TransactionAllocationDto(Guid Id, Guid? CropCycleId, Guid? FieldId,
    string Category, decimal AmountUsd, string AllocationType);
