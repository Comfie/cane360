using Cane360.Domain.Farms;

namespace Cane360.Domain.Labour;

public sealed class WorkerProfile : BaseAuditableEntity
{
    private WorkerProfile() { }

    private WorkerProfile(
        Guid id,
        Guid tenantId,
        Guid farmId,
        Guid personId,
        EmploymentType employmentType,
        DateOnly activeFrom,
        byte[] nationalIdCiphertext,
        byte[] nationalIdNonce,
        byte[] nationalIdTag,
        string nationalIdKeyId,
        byte[] nationalIdFingerprint,
        string nationalIdMask)
    {
        Id = id;
        TenantId = tenantId;
        FarmId = farmId;
        PersonId = personId;
        EmploymentType = employmentType;
        ActiveFrom = activeFrom;
        NationalIdCiphertext = nationalIdCiphertext;
        NationalIdNonce = nationalIdNonce;
        NationalIdTag = nationalIdTag;
        NationalIdKeyId = nationalIdKeyId;
        NationalIdFingerprint = nationalIdFingerprint;
        NationalIdMask = nationalIdMask;
        Status = RecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid PersonId { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public DateOnly ActiveFrom { get; private set; }
    public DateOnly? ActiveTo { get; private set; }
    public RecordStatus Status { get; private set; }
    public byte[] NationalIdCiphertext { get; private set; } = [];
    public byte[] NationalIdNonce { get; private set; } = [];
    public byte[] NationalIdTag { get; private set; } = [];
    public string NationalIdKeyId { get; private set; } = string.Empty;
    public byte[] NationalIdFingerprint { get; private set; } = [];
    public string NationalIdMask { get; private set; } = string.Empty;
    public long Version { get; private set; }

    public static WorkerProfile Create(
        Guid id,
        Guid tenantId,
        Guid farmId,
        Guid personId,
        EmploymentType employmentType,
        DateOnly activeFrom,
        byte[] nationalIdCiphertext,
        byte[] nationalIdNonce,
        byte[] nationalIdTag,
        string nationalIdKeyId,
        byte[] nationalIdFingerprint,
        string nationalIdMask)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || farmId == Guid.Empty || personId == Guid.Empty)
        {
            throw new ArgumentException("Tenant, farm, and person are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(nationalIdKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalIdMask);
        if (nationalIdCiphertext.Length == 0 || nationalIdNonce.Length != 12 ||
            nationalIdTag.Length != 16 || nationalIdFingerprint.Length != 32)
        {
            throw new ArgumentException("Protected national-ID data is invalid.");
        }

        return new WorkerProfile(
            id, tenantId, farmId, personId, employmentType, activeFrom,
            nationalIdCiphertext, nationalIdNonce, nationalIdTag,
            nationalIdKeyId.Trim(), nationalIdFingerprint, nationalIdMask.Trim());
    }

    public void Archive(DateOnly activeTo, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != RecordStatus.Active)
        {
            throw new InvalidOperationException("This worker is already archived.");
        }

        if (activeTo < ActiveFrom)
        {
            throw new InvalidOperationException("The archive date cannot be before the worker start date.");
        }

        ActiveTo = activeTo;
        Status = RecordStatus.Archived;
        Version++;
    }

    public void CorrectNationalId(
        byte[] ciphertext,
        byte[] nonce,
        byte[] tag,
        string keyId,
        byte[] fingerprint,
        string mask,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (ciphertext.Length == 0 || nonce.Length != 12 || tag.Length != 16 || fingerprint.Length != 32)
        {
            throw new ArgumentException("Protected national-ID data is invalid.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mask);
        NationalIdCiphertext = ciphertext;
        NationalIdNonce = nonce;
        NationalIdTag = tag;
        NationalIdKeyId = keyId.Trim();
        NationalIdFingerprint = fingerprint;
        NationalIdMask = mask.Trim();
        Version++;
    }

    public bool IsActiveOn(DateOnly date) =>
        ActiveFrom <= date && (ActiveTo is null || ActiveTo >= date) && Status == RecordStatus.Active;

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This worker changed after it was loaded. Refresh and try again.");
        }
    }
}
