namespace Cane360.Application.Inventory;

public sealed record RedeemManagerInvitationCommand(string Token) : IRequest<TenantSessionDto>;
