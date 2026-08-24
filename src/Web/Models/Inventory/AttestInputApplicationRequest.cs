namespace Cane360.Web.Models.Inventory;

public sealed record AttestInputApplicationRequest(Guid SupervisorPersonId, string? Note, long ExpectedVersion);
