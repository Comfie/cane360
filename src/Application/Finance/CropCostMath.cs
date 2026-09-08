namespace Cane360.Application.Finance;

public static class CropCostMath
{
    public static decimal? PerUnit(decimal totalCostUsd, decimal? denominator) =>
        denominator is > 0
            ? decimal.Round(totalCostUsd / denominator.Value, 2, MidpointRounding.AwayFromZero)
            : null;
}
