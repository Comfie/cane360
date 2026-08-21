namespace Cane360.Domain.Farms;

public static class TenantSecurityRoles
{
    public const string Grower = "Grower";
    public const string FarmManager = "FarmManager";

    public static bool CanManageCropCycles(string role) =>
        role is Grower or FarmManager;
}
