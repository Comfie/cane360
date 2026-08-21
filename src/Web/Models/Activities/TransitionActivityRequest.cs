namespace Cane360.Web.Models.Activities;

public sealed record TransitionActivityRequest(long ExpectedVersion, string? Reason);
