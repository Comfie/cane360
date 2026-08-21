namespace Cane360.Web.Models.Activities;

public sealed record AddSourceReferenceRequest(
    long ExpectedVersion,
    string SourceSheetReference,
    DateOnly CapturedDate);
