namespace Cane360.Web.Models.Activities;

public sealed record DeactivatePersonRequest(long ExpectedVersion, DateOnly ActiveTo);
