using System.Text;
using Cane360.Application.Inventory;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get inventory workspace")]
    [EndpointDescription("Returns inventory workspace for the authenticated farm.")]
    [ProducesResponseType<InventoryWorkspaceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InventoryWorkspaceDto>> Workspace(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetInventoryWorkspaceQuery(), cancellationToken));

    [HttpGet("receipts/{receiptId:guid}")]
    [EndpointSummary("Get stock receipt")]
    [EndpointDescription("Returns stock receipt for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockReceiptDto>> Receipt(Guid receiptId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockReceiptQuery(receiptId), cancellationToken));

    [HttpGet("movements")]
    [EndpointSummary("List stock movements")]
    [EndpointDescription("Returns stock movements for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<StockMovementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> Movements(
        [FromQuery] Guid? itemId, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetStockMovementsQuery(itemId), cancellationToken));
    }

    [HttpGet("counts", Name = "GetCountsInventory")]
    [EndpointSummary("List stock counts")]
    [EndpointDescription("Returns stock counts for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<StockCountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<StockCountDto>>> Counts(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockCountsQuery(), cancellationToken));

    [HttpGet("adjustments", Name = "GetAdjustmentsInventory")]
    [EndpointSummary("List stock adjustments")]
    [EndpointDescription("Returns stock adjustments for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<StockAdjustmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentDto>>> Adjustments(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStockAdjustmentsQuery(), cancellationToken));

    [HttpGet("leakage-report")]
    [EndpointSummary("Get inventory leakage report")]
    [EndpointDescription("Returns inventory leakage report for the authenticated farm.")]
    [ProducesResponseType<LeakageReportDto>(StatusCodes.Status200OK)]
    [EnableRateLimiting(ApiRateLimitOptions.ExportsPolicy)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LeakageReportDto>> LeakageReport([FromQuery] LeakageReportRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOptionalDateOnly(request.FromDate, out var fromDate)) return this.DateValidationError(nameof(request.FromDate));
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ToDate, out var toDate)) return this.DateValidationError(nameof(request.ToDate));
        return Ok(await sender.Send(new GetLeakageReportQuery(new LeakageReportFilter(fromDate, toDate, request.FieldId, request.CropCycleId, request.ActivityId, request.InventoryItemId, request.InventoryLotId, request.IssuerPersonId, request.RecipientPersonId, request.SupervisorPersonId, request.Status, request.ExceptionType, request.Severity, request.Page, request.PageSize)), cancellationToken));
    }

    [HttpGet("leakage-report.csv")]
    [EndpointSummary("Export inventory leakage report")]
    [EndpointDescription("Exports inventory leakage report for the authenticated farm.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EnableRateLimiting(ApiRateLimitOptions.ExportsPolicy)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ExportLeakageReport([FromQuery] LeakageReportRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOptionalDateOnly(request.FromDate, out var fromDate)) return this.DateValidationError(nameof(request.FromDate));
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ToDate, out var toDate)) return this.DateValidationError(nameof(request.ToDate));
        var export = await sender.Send(new ExportLeakageReportCommand(new LeakageReportFilter(fromDate, toDate, request.FieldId, request.CropCycleId, request.ActivityId, request.InventoryItemId, request.InventoryLotId, request.IssuerPersonId, request.RecipientPersonId, request.SupervisorPersonId, request.Status, request.ExceptionType, request.Severity, 1, 500)), cancellationToken);
        return File(Encoding.UTF8.GetBytes(export.Content), "text/csv; charset=utf-8", export.FileName);
    }

    [HttpPost("counts", Name = "CreateCountInventory")]
    [EndpointSummary("Create stock count")]
    [EndpointDescription("Creates stock count for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> CreateCount(CreateStockCountRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out var eventDate)) return this.DateValidationError(nameof(request.EventDate));
        var count = await sender.Send(new CreateStockCountCommand(eventDate, request.Notes ?? string.Empty, request.CountingPersons), cancellationToken);
        return Ok(count);
    }

    [HttpPost("counts/{countId:guid}/start")]
    [EndpointSummary("Start stock count")]
    [EndpointDescription("Starts stock count for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> StartCount(Guid countId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new StartStockCountCommand(countId, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/lines/{lineId:guid}", Name = "EnterStockCountLine")]
    [EndpointSummary("Enter stock count line")]
    [EndpointDescription("Enters stock count line for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> EnterCountLine(Guid countId, Guid lineId, EnterStockCountLineRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EnterStockCountLineCommand(countId, lineId, request.CountedQuantity, request.Notes, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/unexpected-lines")]
    [EndpointSummary("Add unexpected stock count line")]
    [EndpointDescription("Adds unexpected stock count line for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> AddUnexpectedCountLine(Guid countId, AddUnexpectedStockCountLineRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddUnexpectedStockCountLineCommand(countId, request.InventoryItemId, request.InventoryLotId, request.ExpectedCountVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/review")]
    [EndpointSummary("Review stock count")]
    [EndpointDescription("Reviews stock count for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> ReviewCount(Guid countId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReviewStockCountCommand(countId, request.ExpectedVersion), cancellationToken));

    [HttpPost("counts/{countId:guid}/cancel", Name = "CancelCountInventory")]
    [EndpointSummary("Cancel stock count")]
    [EndpointDescription("Cancels stock count for the authenticated farm.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockCountDto>> CancelCount(Guid countId, CancelStockCountRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CancelStockCountCommand(countId, request.ExpectedVersion, request.Reason), cancellationToken));

    [HttpPost("adjustments", Name = "CreateAdjustmentInventory")]
    [EndpointSummary("Create stock adjustment")]
    [EndpointDescription("Creates stock adjustment for the authenticated farm.")]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockAdjustmentDto>> CreateAdjustment(CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out var eventDate)) return this.DateValidationError(nameof(request.EventDate));
        var adjustment = await sender.Send(new CreateStockAdjustmentCommand(request.StockCountLineId, request.InventoryItemId, request.InventoryLotId, request.AdjustmentType, request.SignedQuantity, request.ExplicitUnitValueUsd, request.Reason, eventDate), cancellationToken);
        return Ok(adjustment);
    }

    [HttpPost("adjustments/{adjustmentId:guid}/submit", Name = "SubmitAdjustmentInventory")]
    [EndpointSummary("Submit stock adjustment")]
    [EndpointDescription("Submits stock adjustment for the authenticated farm.")]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockAdjustmentDto>> SubmitAdjustment(Guid adjustmentId, VersionedInventoryRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SubmitStockAdjustmentCommand(adjustmentId, request.ExpectedVersion), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/decision", Name = "DecideAdjustmentInventory")]
    [EndpointSummary("Decide stock adjustment")]
    [EndpointDescription("Records a decision for stock adjustment for the authenticated farm.")]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockAdjustmentDto>> DecideAdjustment(Guid adjustmentId, DecideStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DecideStockAdjustmentCommand(adjustmentId, request.ExpectedVersion, request.Outcome, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/post", Name = "PostAdjustmentInventory")]
    [EndpointSummary("Post stock adjustment")]
    [EndpointDescription("Posts stock adjustment for the authenticated farm.")]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockAdjustmentDto>> PostAdjustment(Guid adjustmentId, PostStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new PostStockAdjustmentCommand(adjustmentId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("adjustments/{adjustmentId:guid}/reverse", Name = "ReverseAdjustmentInventory")]
    [EndpointSummary("Reverse stock adjustment")]
    [EndpointDescription("Reverses stock adjustment for the authenticated farm.")]
    [ProducesResponseType<StockAdjustmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockAdjustmentDto>> ReverseAdjustment(Guid adjustmentId, ReverseStockAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReverseStockAdjustmentCommand(adjustmentId, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("units", Name = "CreateUnitInventory")]
    [EndpointSummary("Create unit of measure")]
    [EndpointDescription("Creates unit of measure for the authenticated farm.")]
    [ProducesResponseType<UnitOfMeasureDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UnitOfMeasureDto>> CreateUnit(
        CreateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        UnitOfMeasureDto result = await sender.Send(new CreateUnitOfMeasureCommand(
            request.Code, request.Name, request.Dimension, request.DecimalPlaces), cancellationToken);
        return Ok(result);
    }

    [HttpGet("units", Name = "GetUnitsInventory")]
    [EndpointSummary("List units of measure")]
    [EndpointDescription("Returns units of measure for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<UnitOfMeasureDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UnitOfMeasureDto>>> Units(
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetUnitsOfMeasureQuery(), cancellationToken));
    }

    [HttpPost("units/{unitId:guid}/archive", Name = "ArchiveUnitInventory")]
    [EndpointSummary("Archive unit of measure")]
    [EndpointDescription("Archives unit of measure for the authenticated farm.")]
    [ProducesResponseType<UnitOfMeasureDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UnitOfMeasureDto>> ArchiveUnit(Guid unitId,
        VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ArchiveUnitOfMeasureCommand(unitId, request.ExpectedVersion),
            cancellationToken));
    }

    [HttpPut("units/{unitId:guid}")]
    [EndpointSummary("Rename unit of measure")]
    [EndpointDescription("Renames unit of measure for the authenticated farm.")]
    [ProducesResponseType<UnitOfMeasureDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UnitOfMeasureDto>> RenameUnit(Guid unitId,
        RenameUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new RenameUnitOfMeasureCommand(unitId,
            request.Name, request.ExpectedVersion), cancellationToken));
    }

    [HttpPost("items")]
    [EndpointSummary("Create inventory item")]
    [EndpointDescription("Creates inventory item for the authenticated farm.")]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryItemDto>> CreateItem(
        CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        InventoryItemDto result = await sender.Send(new CreateInventoryItemCommand(
            request.Code, request.Name, request.Category, request.StockUnitId, request.ReorderLevel,
            request.LotTrackingPolicy, request.ExpiryPolicy), cancellationToken);
        return Ok(result);
    }

    [HttpPost("suppliers")]
    [EndpointSummary("Create supplier")]
    [EndpointDescription("Creates supplier for the authenticated farm.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(
        CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        SupplierDto result = await sender.Send(new CreateSupplierCommand(
            request.Code, request.Name, request.Contact), cancellationToken);
        return Ok(result);
    }

    [HttpPut("suppliers/{supplierId:guid}")]
    [EndpointSummary("Update supplier")]
    [EndpointDescription("Updates supplier for the authenticated farm.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(Guid supplierId,
        UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateSupplierCommand(supplierId, request.Code,
            request.Name, request.Contact, request.ExpectedVersion), cancellationToken));
    }

    [HttpPost("suppliers/{supplierId:guid}/archive", Name = "ArchiveSupplierInventory")]
    [EndpointSummary("Archive supplier")]
    [EndpointDescription("Archives supplier for the authenticated farm.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> ArchiveSupplier(Guid supplierId,
        VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ArchiveSupplierCommand(supplierId, request.ExpectedVersion),
            cancellationToken));
    }

    [HttpPost("suppliers/{supplierId:guid}/unarchive")]
    [EndpointSummary("Unarchive supplier")]
    [EndpointDescription("Unarchives supplier for the authenticated farm.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupplierDto>> UnarchiveSupplier(Guid supplierId,
        VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UnarchiveSupplierCommand(supplierId, request.ExpectedVersion),
            cancellationToken));
    }

    [HttpPost("lots")]
    [EndpointSummary("Create inventory lot")]
    [EndpointDescription("Creates inventory lot for the authenticated farm.")]
    [ProducesResponseType<InventoryLotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryLotDto>> CreateLot(
        CreateInventoryLotRequest request, CancellationToken cancellationToken)
    {
        DateOnly? expiryDate = null;
        if (!TransportValueParser.TryParseOptionalDateOnly(request.ExpiryDate, out expiryDate))
        {
            return this.DateValidationError(nameof(request.ExpiryDate));
        }

        InventoryLotDto result = await sender.Send(new CreateInventoryLotCommand(
            request.InventoryItemId, request.Code, expiryDate), cancellationToken);
        return Ok(result);
    }

    [HttpPost("receipts")]
    [EndpointSummary("Create stock receipt")]
    [EndpointDescription("Creates stock receipt for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockReceiptDto>> CreateReceipt(
        CreateStockReceiptRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ReceiptDate, out DateOnly receiptDate))
        {
            return this.DateValidationError(nameof(request.ReceiptDate));
        }

        StockReceiptDto result = await sender.Send(new CreateStockReceiptCommand(
                request.ReceiptType,
                request.SupplierId,
                receiptDate,
                request.ReceivedByPersonId,
                request.SourceReference,
                request.Reason,
                request.LateEntryReason,
                request.Lines.Select(line => new CreateStockReceiptLineCommand(
                    line.InventoryItemId, line.InventoryLotId, line.Quantity, line.UnitCostUsd)).ToArray()),
            cancellationToken);
        return CreatedAtAction(nameof(Receipt), new { receiptId = result.Id }, result);
    }

    [HttpPost("receipts/{receiptId:guid}/submit-opening-balance")]
    [EndpointSummary("Submit opening balance")]
    [EndpointDescription("Submits opening balance for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockReceiptDto>> SubmitOpeningBalance(
        Guid receiptId, VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new SubmitOpeningBalanceCommand(
            receiptId, request.ExpectedVersion), cancellationToken));
    }

    [HttpPost("receipts/{receiptId:guid}/opening-balance-decision")]
    [EndpointSummary("Decide opening balance")]
    [EndpointDescription("Records a decision for opening balance for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockReceiptDto>> DecideOpeningBalance(
        Guid receiptId, DecideOpeningBalanceRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new DecideOpeningBalanceCommand(
            receiptId, request.ExpectedVersion, request.Outcome, request.Reason,
            request.IdempotencyKey), cancellationToken));
    }

    [HttpPost("receipts/{receiptId:guid}/post", Name = "PostReceiptInventory")]
    [EndpointSummary("Post stock receipt")]
    [EndpointDescription("Posts stock receipt for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockReceiptDto>> PostReceipt(
        Guid receiptId, PostStockReceiptRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new PostStockReceiptCommand(
            receiptId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken));
    }

    [HttpPost("receipts/{receiptId:guid}/reverse", Name = "ReverseReceiptInventory")]
    [EndpointSummary("Reverse stock receipt")]
    [EndpointDescription("Reverses stock receipt for the authenticated farm.")]
    [ProducesResponseType<StockReceiptDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockReceiptDto>> ReverseReceipt(
        Guid receiptId, ReverseStockReceiptRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ReverseStockReceiptCommand(
            receiptId, request.ExpectedVersion, request.Reason, request.IdempotencyKey), cancellationToken));
    }
}
