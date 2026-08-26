using System.Text;
using System.Text.Json;
using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class ExportLeakageReportCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, ISender sender, TimeProvider timeProvider) : IRequestHandler<ExportLeakageReportCommand, LeakageCsvExportDto>
{
    public async Task<LeakageCsvExportDto> Handle(ExportLeakageReportCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user);
        var fullFilter = command.Filter with { Page = 1, PageSize = 500 }; var report = await sender.Send(new GetLeakageReportQuery(fullFilter), cancellationToken); var rows = report.Rows.ToList();
        for (var page = 2; rows.Count < report.TotalRows; page++)
            rows.AddRange((await sender.Send(new GetLeakageReportQuery(fullFilter with { Page = page }), cancellationToken)).Rows);
        var now = timeProvider.GetUtcNow(); var snapshot = JsonSerializer.Serialize(fullFilter);
        var export = InventoryLeakageExport.Create(tenant.Id, farm.Id, snapshot, userId, now); inventoryRepository.Add(export);
        var audit = AuditEvent.Create(tenant.Id, farm.Id, nameof(InventoryLeakageExport), export.Id, "Exported", userId, InventoryAccess.SecurityRole(tenant, userId), null, now, InventoryAccess.CorrelationId(user), null, "Exported the complete authorised leakage-report result with its exact filter snapshot."); inventoryRepository.Add(audit); inventoryRepository.Add(InventoryAuditEventLink.ForLeakageExport(audit.Id, tenant.Id, farm.Id, export.Id));
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        var csv = new StringBuilder(); csv.AppendLine($"Cane360 leakage report,Generated {now:O},Farm {Escape(farm.Name)},USD"); csv.AppendLine($"Filters,{Escape(snapshot)}"); csv.AppendLine("Exception type,Severity,Status,Event date,Item,Lot,Quantity,Unit,Value USD,Source chain,Trace");
        foreach (var row in rows) csv.AppendLine(string.Join(',', Escape(row.ExceptionType), Escape(row.Severity), Escape(row.Status), row.EventDate.ToString("yyyy-MM-dd"), Escape(row.InventoryItemId?.ToString("N")[..8]), Escape(row.InventoryLotId?.ToString("N")[..8]), row.Quantity.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture), Escape(row.UnitCode), row.ValueUsd.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture), Escape(string.Join("/", row.SourceChainIds.Select(id => id.ToString("N")[..8]))), Escape(row.TraceSummary)));
        return new LeakageCsvExportDto(csv.ToString(), $"cane360-leakage-{now:yyyyMMdd-HHmmss}.csv");
    }
    private static string Escape(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
