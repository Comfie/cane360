namespace Cane360.Web.Infrastructure;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";
    public const string UsersPolicy = "users";
    public const string ExportsPolicy = "exports";

    public int UsersPermitLimit { get; set; } = 20;
    public int UsersWindowSeconds { get; set; } = 60;
    public int ExportsPermitLimit { get; set; } = 30;
    public int ExportsWindowSeconds { get; set; } = 60;
}
