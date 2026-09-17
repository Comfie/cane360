namespace Cane360.Application.Common.Interfaces;

/// <summary>Scalar session facts for a signed-in tenant member, projected without loading the tenant aggregate.</summary>
public sealed record TenantSessionSummary(string SecurityRole, string? TenantCode, string? FarmName);
