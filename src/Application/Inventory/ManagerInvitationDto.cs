namespace Cane360.Application.Inventory;

public sealed record ManagerInvitationDto(
    Guid Id, Guid PersonId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt,
    DateTimeOffset? RedeemedAt, long Version);
