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
    Active,
    ReadyForHarvest,
    Harvested,
    Closed,
    Cancelled
}
