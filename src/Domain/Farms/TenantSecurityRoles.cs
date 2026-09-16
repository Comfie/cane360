namespace Cane360.Domain.Farms;

public static class TenantSecurityRoles
{
    public const string Grower = "Grower";
    public const string FarmManager = "FarmManager";
    public const string Supervisor = "Supervisor";

    public static bool CanManageCropCycles(string role) =>
        role is Grower or FarmManager;

    public static bool IsInvitable(string role) =>
        role is FarmManager or Supervisor;
}
