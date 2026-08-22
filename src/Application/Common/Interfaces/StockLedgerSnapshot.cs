using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Common.Interfaces;

public sealed record StockLedgerSnapshot(decimal Quantity, decimal ValueUsd)
{
    public decimal WeightedAverageUnitCostUsd => Quantity == 0
        ? 0
        : decimal.Round(ValueUsd / Quantity, 6, MidpointRounding.AwayFromZero);
}
