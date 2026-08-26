namespace Cane360.Application.Inventory;

public sealed record GetLeakageReportQuery(LeakageReportFilter Filter) : IRequest<LeakageReportDto>;
