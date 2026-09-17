namespace Cane360.Application.Session;

public sealed record SessionSummaryDto(bool HasTenant, string? Role, string? TenantCode, string? FarmName);
