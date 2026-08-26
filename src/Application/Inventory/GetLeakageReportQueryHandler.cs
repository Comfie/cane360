using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class GetLeakageReportQueryHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetLeakageReportQuery, LeakageReportDto>
{
    public async Task<LeakageReportDto> Handle(GetLeakageReportQuery query, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant);
        var source = await inventoryRepository.GetLeakageReportingSourceAsync(tenant.Id, farm.Id, cancellationToken); var rows = new List<LeakageReportRowDto>();
        var issuesByLine = source.Issues.SelectMany(issue => issue.Lines.Select(line => (line.Id, Issue: issue))).ToDictionary(value => value.Id, value => value.Issue);
        var requestsById = source.Requests.ToDictionary(request => request.Id);
        var receiptsByLine = source.FieldReceipts.SelectMany(receipt => receipt.Lines.Select(line => (line.Id, Receipt: receipt))).ToDictionary(value => value.Id, value => value.Receipt);
        var adjustmentsById = source.Adjustments.ToDictionary(adjustment => adjustment.Id);
        var approvalIdsByAdjustment = source.Approvals.Where(approval => approval.StockAdjustmentId.HasValue)
            .GroupBy(approval => approval.StockAdjustmentId!.Value).ToDictionary(group => group.Key, group => group.Select(approval => approval.Id).ToArray());
        LeakageReportTrace Trace(Guid issueLineId, Guid? fieldReceiptLineId = null)
        {
            if (!issuesByLine.TryGetValue(issueLineId, out var issue)) return LeakageReportTrace.Empty;
            requestsById.TryGetValue(issue.InputRequestId, out var request);
            if (fieldReceiptLineId.HasValue && receiptsByLine.TryGetValue(fieldReceiptLineId.Value, out var receipt))
                return new LeakageReportTrace(receipt.FieldId, receipt.CropCycleId, receipt.ActivityId, issue.IssuerPersonId, receipt.RecipientPersonId);
            return new LeakageReportTrace(request?.FieldId, request?.CropCycleId, request?.ActivityId, issue.IssuerPersonId, issue.RecipientPersonId);
        }
        foreach (var exception in source.Exceptions)
        {
            var trace = Trace(exception.StockIssueLineId);
            rows.Add(new("UnaccountedIssue", "High", exception.Status.ToString(), DateOnly.FromDateTime(exception.OpenedAt.UtcDateTime), trace.FieldId, trace.CropCycleId, exception.ActivityId, null, null, trace.IssuerPersonId, trace.RecipientPersonId, null, exception.UnaccountedQuantity, 0, "", [exception.Id, exception.StockIssueLineId, exception.ActivityId], "Issue → field receipt → application/return/loss accountability exception."));
        }
        foreach (var application in source.Applications)
        {
            var applicationTrace = application.Lines.Select(line => Trace(line.StockIssueLineId, line.FieldReceiptLineId)).FirstOrDefault() ?? LeakageReportTrace.Empty;
            if (application.IsLateConfirmation) rows.Add(new("LateConfirmation", "Medium", application.Status.ToString(), DateOnly.FromDateTime(application.AppliedAt.UtcDateTime), applicationTrace.FieldId, applicationTrace.CropCycleId, application.ActivityId, null, null, applicationTrace.IssuerPersonId, applicationTrace.RecipientPersonId, application.SupervisorPersonId, 0, 0, "", [application.Id, application.ActivityId], "Issue → field receipt → application → late manager confirmation."));
            foreach (var line in application.Lines.Where(line => line.RateVariance != 0)) { var trace = Trace(line.StockIssueLineId, line.FieldReceiptLineId); rows.Add(new("ApplicationRateVariance", "Medium", application.Status.ToString(), DateOnly.FromDateTime(application.AppliedAt.UtcDateTime), trace.FieldId, trace.CropCycleId, application.ActivityId, line.InventoryItemId, line.InventoryLotId, trace.IssuerPersonId, trace.RecipientPersonId, application.SupervisorPersonId, line.RateVariance, line.AppliedQuantity * line.IssueUnitCostUsdSnapshot, line.UnitCodeSnapshot, [application.Id, line.Id, line.StockIssueLineId, line.FieldReceiptLineId], "Issue → field receipt → application rule snapshot → rate variance.")); }
        }
        foreach (var loss in source.Losses.Where(loss => loss.Status == InventoryLossStatus.Approved)) { var trace = Trace(loss.StockIssueLineId); rows.Add(new("ApprovedFieldLoss", "High", loss.Status.ToString(), DateOnly.FromDateTime(loss.Created.UtcDateTime), trace.FieldId, trace.CropCycleId, loss.ActivityId, loss.InventoryItemId, loss.InventoryLotId, trace.IssuerPersonId, trace.RecipientPersonId, null, loss.Quantity, loss.Quantity * loss.IssueUnitCostUsdSnapshot, loss.UnitCodeSnapshot, [loss.Id, loss.StockIssueLineId, loss.ActivityId], "Issue → field receipt → Grower-approved field loss.")); }
        foreach (var count in source.Counts) foreach (var line in count.Lines.Where(line => line.VarianceQuantity != 0))
        {
            var chain = new List<Guid> { count.Id, line.Id };
            if (line.PostedStockAdjustmentId.HasValue && adjustmentsById.TryGetValue(line.PostedStockAdjustmentId.Value, out var adjustment))
            {
                chain.Add(adjustment.Id); if (adjustment.StockMovementId.HasValue) chain.Add(adjustment.StockMovementId.Value);
                if (approvalIdsByAdjustment.TryGetValue(adjustment.Id, out var approvalIds)) chain.AddRange(approvalIds);
            }
            rows.Add(new("StockCountVariance", line.IsResolved ? "Resolved" : "High", count.Status.ToString(), count.EventDate, null, null, null, line.InventoryItemId, line.InventoryLotId, null, null, null, line.VarianceQuantity, line.ExpectedQuantity == 0 ? 0 : line.VarianceQuantity * (line.ExpectedValueUsd / line.ExpectedQuantity), line.UnitCodeSnapshot, chain.ToArray(), "Count cut-off → immutable expected snapshot → counted quantity → variance → Grower approval → adjustment movement."));
        }
        foreach (var adjustment in source.Adjustments)
        {
            var chain = new List<Guid> { adjustment.Id }; if (adjustment.StockCountLineId.HasValue) chain.Add(adjustment.StockCountLineId.Value); if (adjustment.StockMovementId.HasValue) chain.Add(adjustment.StockMovementId.Value); if (approvalIdsByAdjustment.TryGetValue(adjustment.Id, out var approvalIds)) chain.AddRange(approvalIds); if (adjustment.ReversalOfStockAdjustmentId.HasValue) chain.Add(adjustment.ReversalOfStockAdjustmentId.Value); if (adjustment.ReversalStockAdjustmentId.HasValue) chain.Add(adjustment.ReversalStockAdjustmentId.Value);
            rows.Add(new("StockAdjustment", adjustment.Status is StockAdjustmentStatus.Rejected ? "High" : adjustment.Status is StockAdjustmentStatus.PendingGrowerApproval ? "Medium" : "Resolved", adjustment.Status.ToString(), adjustment.EventDate, null, null, null, adjustment.InventoryItemId, adjustment.InventoryLotId, null, null, null, adjustment.SignedQuantity, adjustment.SignedValueUsdSnapshot ?? 0, adjustment.UnitCodeSnapshot, chain.ToArray(), "Adjustment draft/approval → signed movement → correction or reversal."));
        }
        var filtered = rows.Where(row => Matches(row, query.Filter)).OrderByDescending(row => row.EventDate).ToArray(); var page = Math.Max(1, query.Filter.Page); var size = Math.Clamp(query.Filter.PageSize, 1, 500);
        return new LeakageReportDto(query.Filter, filtered.Length, filtered.Sum(row => row.Quantity), filtered.Sum(row => row.ValueUsd), filtered.Skip((page - 1) * size).Take(size).ToArray());
    }

    private static bool Matches(LeakageReportRowDto row, LeakageReportFilter filter) =>
        (!filter.FromDate.HasValue || row.EventDate >= filter.FromDate) && (!filter.ToDate.HasValue || row.EventDate <= filter.ToDate) &&
        (!filter.FieldId.HasValue || row.FieldId == filter.FieldId) && (!filter.CropCycleId.HasValue || row.CropCycleId == filter.CropCycleId) && (!filter.ActivityId.HasValue || row.ActivityId == filter.ActivityId) &&
        (!filter.InventoryItemId.HasValue || row.InventoryItemId == filter.InventoryItemId) && (!filter.InventoryLotId.HasValue || row.InventoryLotId == filter.InventoryLotId) &&
        (!filter.IssuerPersonId.HasValue || row.IssuerPersonId == filter.IssuerPersonId) && (!filter.RecipientPersonId.HasValue || row.RecipientPersonId == filter.RecipientPersonId) && (!filter.SupervisorPersonId.HasValue || row.SupervisorPersonId == filter.SupervisorPersonId) &&
        (string.IsNullOrWhiteSpace(filter.Status) || row.Status.Equals(filter.Status, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(filter.ExceptionType) || row.ExceptionType.Equals(filter.ExceptionType, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(filter.Severity) || row.Severity.Equals(filter.Severity, StringComparison.OrdinalIgnoreCase));
}
