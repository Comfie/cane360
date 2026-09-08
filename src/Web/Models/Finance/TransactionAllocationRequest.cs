namespace Cane360.Web.Models.Finance;

public sealed record TransactionAllocationRequest(Guid? CropCycleId, Guid? FieldId,
    string Category, decimal AmountUsd, string AllocationType);
