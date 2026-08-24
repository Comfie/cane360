namespace Cane360.Web.Models.Activities;

public sealed record CreateActivityTypeRequest(
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis);
