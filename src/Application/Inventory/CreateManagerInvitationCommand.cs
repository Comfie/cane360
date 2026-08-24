namespace Cane360.Application.Inventory;

public sealed record CreateManagerInvitationCommand(Guid PersonId, int ExpiresInHours) : IRequest<CreatedManagerInvitationDto>;
