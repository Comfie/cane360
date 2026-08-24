namespace Cane360.Domain.Farms;

public sealed class ManagerInvitation : BaseAuditableEntity
{
    private ManagerInvitation() { }

    private ManagerInvitation(Guid tenantId, Guid farmId, Guid personId, string tokenHash,
        DateTimeOffset expiresAt, string createdByUserId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        PersonId = personId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByUserId = createdByUserId.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid PersonId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByUserId { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public string? RedeemedByUserId { get; private set; }
    public long Version { get; private set; }

    public static ManagerInvitation Create(Guid tenantId, Guid farmId, Guid personId,
        string tokenHash, DateTimeOffset expiresAt, string createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        return new(tenantId, farmId, personId, tokenHash, expiresAt, createdByUserId);
    }

    public void Revoke(DateTimeOffset revokedAt, string userId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (RedeemedAt.HasValue) throw new InvalidOperationException("A redeemed invitation cannot be revoked.");
        if (RevokedAt.HasValue) return;
        RevokedAt = revokedAt;
        RevokedByUserId = userId.Trim();
        Version++;
    }

    public void Redeem(DateTimeOffset redeemedAt, string userId)
    {
        if (RevokedAt.HasValue) throw new InvalidOperationException("This manager invitation was revoked.");
        if (RedeemedAt.HasValue) throw new InvalidOperationException("This manager invitation has already been used.");
        if (redeemedAt > ExpiresAt) throw new InvalidOperationException("This manager invitation has expired.");
        RedeemedAt = redeemedAt;
        RedeemedByUserId = userId.Trim();
        Version++;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This invitation changed after it was loaded.");
    }
}
