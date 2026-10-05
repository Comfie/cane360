namespace Cane360.Domain.Farms;

public sealed class GrowerProfile : BaseAuditableEntity
{
    private GrowerProfile() { }

    private GrowerProfile(Guid tenantId, string displayName, string? phone)
    {
        TenantId = tenantId;
        DisplayName = displayName;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
    }

    public Guid TenantId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }

    public string? Title { get; private set; }
    public string? FirstName { get; private set; }
    public string? Surname { get; private set; }
    public string? Sex { get; private set; }
    public string? GrowerNumber { get; private set; }
    public string? Association { get; private set; }
    public string? MembershipNumber { get; private set; }
    public string? RegisteredAddress { get; private set; }
    public string? Email { get; private set; }
    public string? PhotoReference { get; private set; }
    public bool Active { get; private set; } = true;
    public byte[]? NationalIdCiphertext { get; private set; }
    public byte[]? NationalIdNonce { get; private set; }
    public byte[]? NationalIdTag { get; private set; }
    public string? NationalIdKeyId { get; private set; }
    public byte[]? NationalIdFingerprint { get; private set; }
    public string? NationalIdMask { get; private set; }

    public void UpdateProfile(string? title, string? firstName, string? surname, string? sex,
        string? growerNumber, string? association, string? membershipNumber, string? registeredAddress,
        string? email, string? photoReference, bool active)
    {
        Title = Clean(title, 40);
        FirstName = Clean(firstName, 100);
        Surname = Clean(surname, 100);
        Sex = Clean(sex, 40);
        GrowerNumber = Clean(growerNumber, 60);
        Association = Clean(association, 120);
        MembershipNumber = Clean(membershipNumber, 60);
        RegisteredAddress = Clean(registeredAddress, 240);
        Email = Clean(email, 254);
        PhotoReference = Clean(photoReference, 240);
        Active = active;
    }

    public void SetNationalId(byte[] ciphertext, byte[] nonce, byte[] tag, string keyId,
        byte[] fingerprint, string mask)
    {
        if (ciphertext.Length == 0 || nonce.Length != 12 || tag.Length != 16 || fingerprint.Length != 32)
        {
            throw new ArgumentException("Protected national-ID data is invalid.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mask);
        NationalIdCiphertext = ciphertext;
        NationalIdNonce = nonce;
        NationalIdTag = tag;
        NationalIdKeyId = keyId;
        NationalIdFingerprint = fingerprint;
        NationalIdMask = mask;
    }

    private static string? Clean(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Trim().Length > maximum)
        {
            throw new ArgumentException("Profile field exceeds supported length.");
        }
        return value.Trim();
    }

    internal static GrowerProfile Create(Guid tenantId, string displayName, string? phone)
    {
        return new GrowerProfile(tenantId, displayName, phone);
    }

    public void Update(string displayName, string? phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
