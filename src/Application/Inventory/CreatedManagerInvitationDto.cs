namespace Cane360.Application.Inventory;

public sealed record CreatedManagerInvitationDto(
    Guid Id, Guid PersonId, DateTimeOffset ExpiresAt, long Version, string Token);
