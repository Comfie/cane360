namespace Cane360.Application.Administration;

public sealed record AdministrationSessionDto(
    string Role, string TenantCode, string FarmName);
