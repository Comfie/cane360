namespace Cane360.Domain.Farms;

public enum RecordStatus
{
    Active,
    Archived
}

public enum ReportingAreaSource
{
    Declared,
    Mapped
}

public enum CropCycleType
{
    PlantCane,
    Ratoon
}

public enum CropCycleStatus
{
    Draft,
    Active,
    ReadyForHarvest,
    Harvested,
    Closed,
    Cancelled
}

public static class TenantSecurityRoles
{
    public const string Grower = "Grower";
    public const string FarmManager = "FarmManager";

    public static bool CanManageCropCycles(string role) =>
        role is Grower or FarmManager;
}
