using System.Globalization;
using System.Text;
using System.Text.Json;
using Cane360.Application.Common.Models;
using Cane360.Domain.Auditing;

namespace Cane360.Application.Inventory;

public sealed class ExportLeakageReportCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    ISender sender,
    TimeProvider timeProvider) : IRequestHandler<ExportLeakageReportCommand, LeakageCsvExportDto>
{
    public async Task<LeakageCsvExportDto> Handle(ExportLeakageReportCommand command,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        LeakageReportFilter fullFilter = command.Filter with { Page = 1, PageSize = 500 };
        LeakageReportDto report = await sender.Send(new GetLeakageReportQuery(fullFilter), cancellationToken);
        List<LeakageReportRowDto> rows = report.Rows.ToList();
        for (int page = 2; rows.Count < report.TotalRows; page++)
        {
            rows.AddRange((await sender.Send(new GetLeakageReportQuery(fullFilter with { Page = page }),
                cancellationToken)).Rows);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string snapshot = JsonSerializer.Serialize(fullFilter);
        InventoryLeakageExport export = InventoryLeakageExport.Create(tenant.Id, farm.Id, snapshot, userId, now);
        inventoryRepository.Add(export);
        AuditEvent audit = AuditEvent.Create(tenant.Id, farm.Id, nameof(InventoryLeakageExport), export.Id, "Exported",
            userId, InventoryAccess.SecurityRole(tenant, userId), null, now, InventoryAccess.CorrelationId(user), null,
            "Exported the complete authorised leakage-report result with its exact filter snapshot.");
        inventoryRepository.Add(audit);
        inventoryRepository.Add(InventoryAuditEventLink.ForLeakageExport(audit.Id, tenant.Id, farm.Id, export.Id));
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        StringBuilder csv = new();
        csv.AppendLine("Report,Cane360 leakage report");
        csv.AppendLine($"Farm,{Escape(farm.Name)}");
        csv.AppendLine($"Generated UTC,{now:O}");
        csv.AppendLine($"Filters,{Escape(snapshot)}");
        csv.AppendLine("Source,Authoritative leakage-report query; Currency USD");
        csv.AppendLine("Exception type,Severity,Status,Event date,Item,Lot,Quantity,Unit,Value USD,Source chain,Trace");
        foreach (LeakageReportRowDto row in rows)
        {
            csv.AppendLine(string.Join(',', Escape(row.ExceptionType), Escape(row.Severity), Escape(row.Status),
                row.EventDate.ToString("yyyy-MM-dd"), Escape(row.InventoryItemId?.ToString("N")[..8]),
                Escape(row.InventoryLotId?.ToString("N")[..8]),
                row.Quantity.ToString("0.######", CultureInfo.InvariantCulture), Escape(row.UnitCode),
                row.ValueUsd.ToString("0.######", CultureInfo.InvariantCulture),
                Escape(string.Join("/", row.SourceChainIds.Select(id => id.ToString("N")[..8]))),
                Escape(row.TraceSummary)));
        }

        return new LeakageCsvExportDto(csv.ToString(), $"cane360-leakage-{now:yyyyMMdd-HHmmss}.csv");
    }

    private static string Escape(string? value)
    {
        return CsvCell.Text(value);
    }
}
