namespace Cane360.Web.Models.Inventory;

public sealed record CreateManagerInvitationRequest(Guid PersonId, int ExpiresInHours);
