using Cane360.Application.Inventory;
using Cane360.Web.Models.Inventory;
using MediatR;
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
