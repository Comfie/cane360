namespace Cane360.Domain.Finance;

public sealed class TransactionAllocation : BaseEntity
{
    private TransactionAllocation() { }

    private TransactionAllocation(Guid tenantId, Guid farmId, Guid transactionId,
        Guid? cropCycleId, Guid? fieldId, OperationalFinanceCategory category,
        decimal amountUsd, TransactionAllocationType allocationType, DateTimeOffset createdAt)
    {
        if (tenantId == Guid.Empty || farmId == Guid.Empty || transactionId == Guid.Empty)
            throw new ArgumentException("Tenant, farm, and transaction are required.");
        if (amountUsd <= 0) throw new ArgumentOutOfRangeException(nameof(amountUsd), "Allocation amount must be positive.");
        if (decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero) != amountUsd)
            throw new ArgumentException("USD allocations support at most two decimal places.", nameof(amountUsd));
        if (allocationType == TransactionAllocationType.CropCycleDirect && (cropCycleId is null || fieldId is null))
            throw new InvalidOperationException("A crop-cycle direct allocation requires its field and crop cycle.");
        if (allocationType == TransactionAllocationType.Field && (fieldId is null || cropCycleId is not null))
            throw new InvalidOperationException("A field allocation requires only a field.");
        if (allocationType == TransactionAllocationType.FarmOverhead && (fieldId is not null || cropCycleId is not null))
            throw new InvalidOperationException("Farm overhead cannot identify a field or crop cycle.");

        TenantId = tenantId;
        FarmId = farmId;
        OperationalTransactionId = transactionId;
        CropCycleId = cropCycleId;
        FieldId = fieldId;
        Category = category;
        AmountUsd = amountUsd;
        AllocationType = allocationType;
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid OperationalTransactionId { get; private set; }
    public Guid? CropCycleId { get; private set; }
    public Guid? FieldId { get; private set; }
    public OperationalFinanceCategory Category { get; private set; }
    public decimal AmountUsd { get; private set; }
    public TransactionAllocationType AllocationType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static TransactionAllocation Create(Guid tenantId, Guid farmId, Guid transactionId,
        Guid? cropCycleId, Guid? fieldId, OperationalFinanceCategory category, decimal amountUsd,
        TransactionAllocationType allocationType, DateTimeOffset createdAt) =>
        new(tenantId, farmId, transactionId, cropCycleId, fieldId, category, amountUsd,
            allocationType, createdAt);
}
