namespace Cane360.Web.Models.Administration;

public sealed record EndEffectiveRuleRequest(DateOnly EffectiveTo, long ExpectedVersion);
