namespace Cane360.Application.CropCycles;

public sealed class CropMaturityOptions
{
    public const string SectionName = "CropCycles";
    public int DefaultCropMaturityMonths { get; set; } = 14;

    public DateOnly? Calculate(DateOnly? plantingDate)
    {
        return plantingDate?.AddMonths(DefaultCropMaturityMonths);
    }
}
