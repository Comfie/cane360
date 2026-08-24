namespace Cane360.Web.Models.Inventory;

public sealed record CreateInputRequestRequest(
    Guid ActivityId, IReadOnlyList<CreateInputRequestLineRequest> Lines);
