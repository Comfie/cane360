namespace Cane360.Web.Models.Inventory;

public sealed record CreateUnitOfMeasureRequest(
    string Code, string Name, string Dimension, int DecimalPlaces);
