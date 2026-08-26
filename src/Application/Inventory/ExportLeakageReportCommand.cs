namespace Cane360.Application.Inventory;

public sealed record ExportLeakageReportCommand(LeakageReportFilter Filter) : IRequest<LeakageCsvExportDto>;
