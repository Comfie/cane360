namespace Cane360.Application.Inventory;

public sealed record LeakageReportDto(LeakageReportFilter Filter, int TotalRows, decimal TotalQuantity,
    decimal TotalValueUsd, IReadOnlyList<LeakageReportRowDto> Rows);
