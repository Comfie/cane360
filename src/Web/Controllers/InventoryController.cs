using Cane360.Application.Inventory;
using Cane360.Web.Models.Inventory;
using MediatR;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<InventoryWorkspaceDto>> Workspace(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetInventoryWorkspaceQuery(), cancellationToken));

    [HttpGet("receipts/{receiptId:guid}")]
    public async Task<ActionResult<StockReceiptDto>> Receipt(Guid receiptId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockReceiptQuery(receiptId), cancellationToken));

    [HttpGet("movements")]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> Movements(
        [FromQuery] Guid? itemId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockMovementsQuery(itemId), cancellationToken));

    [HttpGet("counts")]
    public async Task<ActionResult<IReadOnlyList<StockCountDto>>> Counts(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockCountsQuery(), cancellationToken));

    [HttpGet("adjustments")]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentDto>>> Adjustments(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockAdjustmentsQuery(), cancellationToken));

    [HttpGet("leakage-report")]
    public async Task<ActionResult<LeakageReportDto>> LeakageReport([FromQuery] LeakageReportRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOptionalDateOnly(request.FromDate, out var fromDate)) return BadRequest(DateError(nameof(request.FromDate)));
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ToDate, out var toDate)) return BadRequest(DateError(nameof(request.ToDate)));
        return Ok(await sender.Send(new GetLeakageReportQuery(new LeakageReportFilter(fromDate, toDate, request.FieldId, request.CropCycleId, request.ActivityId, request.InventoryItemId, request.InventoryLotId, request.IssuerPersonId, request.RecipientPersonId, request.SupervisorPersonId, request.Status, request.ExceptionType, request.Severity, request.Page, request.PageSize)), cancellationToken));
    }

    [HttpGet("leakage-report.csv")]
    public async Task<IActionResult> ExportLeakageReport([FromQuery] LeakageReportRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOptionalDateOnly(request.FromDate, out var fromDate)) return BadRequest(DateError(nameof(request.FromDate)));
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ToDate, out var toDate)) return BadRequest(DateError(nameof(request.ToDate)));
        var export = await sender.Send(new ExportLeakageReportCommand(new LeakageReportFilter(fromDate, toDate, request.FieldId, request.CropCycleId, request.ActivityId, request.InventoryItemId, request.InventoryLotId, request.IssuerPersonId, request.RecipientPersonId, request.SupervisorPersonId, request.Status, request.ExceptionType, request.Severity, 1, 500)), cancellationToken);
        return File(Encoding.UTF8.GetBytes(export.Content), "text/csv; charset=utf-8", export.FileName);
    }

    [HttpPost("counts")]
    public async Task<ActionResult<StockCountDto>> CreateCount(CreateStockCountRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out var eventDate)) return BadRequest(DateError(nameof(request.EventDate)));
        var count = await sender.Send(new CreateStockCountCommand(eventDate, request.Notes ?? string.Empty, request.CountingPersons), cancellationToken);
        return CreatedAtAction(nameof(Workspace), count);
    }

    [HttpPost("counts/{countId:guid}/start")]
    public async Task<ActionResult<StockCountDto>> StartCount(Guid countId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new StartStockCountCommand(countId, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<StockCountDto>> EnterCountLine(Guid countId, Guid lineId, EnterStockCountLineRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EnterStockCountLineCommand(countId, lineId, request.CountedQuantity, request.Notes, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/unexpected-lines")]
    public async Task<ActionResult<StockCountDto>> AddUnexpectedCountLine(Guid countId, AddUnexpectedStockCountLineRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddUnexpectedStockCountLineCommand(countId, request.InventoryItemId, request.InventoryLotId, request.ExpectedCountVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/review")]
    public async Task<ActionResult<StockCountDto>> ReviewCount(Guid countId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReviewStockCountCommand(countId, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/cancel")]
    public async Task<ActionResult<StockCountDto>> CancelCount(Guid countId, CancelStockCountRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelStockCountCommand(countId, request.ExpectedVersion, request.Reason), cancellationToken));

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockAdjustmentDto>> CreateAdjustment(CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out var eventDate)) return BadRequest(DateError(nameof(request.EventDate)));
        var adjustment = await sender.Send(new CreateStockAdjustmentCommand(request.StockCountLineId, request.InventoryItemId, request.InventoryLotId, request.AdjustmentType, request.SignedQuantity, request.ExplicitUnitValueUsd, request.Reason, eventDate), cancellationToken);
        return CreatedAtAction(nameof(Workspace), adjustment);
    }

    [HttpPost("adjustments/{adjustmentId:guid}/submit")]
    public async Task<ActionResult<StockAdjustmentDto>> SubmitAdjustment(Guid adjustmentId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SubmitStockAdjustmentCommand(adjustmentId, request.ExpectedVersion), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/decision")]
    public async Task<ActionResult<StockAdjustmentDto>> DecideAdjustment(Guid adjustmentId, DecideStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DecideStockAdjustmentCommand(adjustmentId, request.ExpectedVersion, request.Outcome, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/post")]
    public async Task<ActionResult<StockAdjustmentDto>> PostAdjustment(Guid adjustmentId, PostStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PostStockAdjustmentCommand(adjustmentId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/reverse")]
    public async Task<ActionResult<StockAdjustmentDto>> ReverseAdjustment(Guid adjustmentId, ReverseStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReverseStockAdjustmentCommand(adjustmentId, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("units")]
    public async Task<ActionResult<UnitOfMeasureDto>> CreateUnit(
        CreateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUnitOfMeasureCommand(
            request.Code, request.Name, request.Dimension, request.DecimalPlaces), cancellationToken);
        return CreatedAtAction(nameof(Workspace), result);
    }

    [HttpPost("items")]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateInventoryItemCommand(
            request.Code, request.Name, request.Category, request.StockUnitId, request.ReorderLevel,
            request.LotTrackingPolicy, request.ExpiryPolicy), cancellationToken);
        return CreatedAtAction(nameof(Workspace), result);
    }

    [HttpPost("suppliers")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(
        CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSupplierCommand(
            request.Code, request.Name, request.Contact), cancellationToken);
        return CreatedAtAction(nameof(Workspace), result);
    }

    [HttpPost("lots")]
    public async Task<ActionResult<InventoryLotDto>> CreateLot(
        CreateInventoryLotRequest request, CancellationToken cancellationToken)
    {
        DateOnly? expiryDate = null;
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ExpiryDate, out expiryDate))
        {
            return BadRequest(DateError(nameof(request.ExpiryDate)));
        }
        var result = await sender.Send(new CreateInventoryLotCommand(
            request.InventoryItemId, request.Code, expiryDate), cancellationToken);
        return CreatedAtAction(nameof(Workspace), result);
    }

    [HttpPost("receipts")]
    public async Task<ActionResult<StockReceiptDto>> CreateReceipt(
        CreateStockReceiptRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ReceiptDate, out var receiptDate))
        {
            return BadRequest(DateError(nameof(request.ReceiptDate)));
        }
        var result = await sender.Send(new CreateStockReceiptCommand(
            request.ReceiptType,
            request.SupplierId,
            receiptDate,
            request.ReceivedByPersonId,
            request.SourceReference,
            request.Reason,
            request.LateEntryReason,
            request.Lines.Select(line => new CreateStockReceiptLineCommand(
                line.InventoryItemId, line.InventoryLotId, line.Quantity, line.UnitCostUsd)).ToArray()), cancellationToken);
        return CreatedAtAction(nameof(Receipt), new { receiptId = result.Id }, result);
    }

    [HttpPost("receipts/{receiptId:guid}/submit-opening-balance")]
    public async Task<ActionResult<StockReceiptDto>> SubmitOpeningBalance(
        Guid receiptId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SubmitOpeningBalanceCommand(
            receiptId, request.ExpectedVersion), cancellationToken));

    [HttpPost("receipts/{receiptId:guid}/opening-balance-decision")]
    public async Task<ActionResult<StockReceiptDto>> DecideOpeningBalance(
        Guid receiptId, DecideOpeningBalanceRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DecideOpeningBalanceCommand(
            receiptId, request.ExpectedVersion, request.Outcome, request.Reason,
            request.IdempotencyKey), cancellationToken));

    [HttpPost("receipts/{receiptId:guid}/post")]
    public async Task<ActionResult<StockReceiptDto>> PostReceipt(
        Guid receiptId, PostStockReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PostStockReceiptCommand(
            receiptId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("receipts/{receiptId:guid}/reverse")]
    public async Task<ActionResult<StockReceiptDto>> ReverseReceipt(
        Guid receiptId, ReverseStockReceiptRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReverseStockReceiptCommand(
            receiptId, request.ExpectedVersion, request.Reason, request.IdempotencyKey), cancellationToken));

    private static ValidationProblemDetails DateError(string propertyName) => new(
        new Dictionary<string, string[]> { [propertyName] = ["Date must use yyyy-MM-dd."] });
}
