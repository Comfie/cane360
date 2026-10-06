using Cane360.Application.Inventory;
using Cane360.Web.Models.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory/categories")]
public sealed class InventoryCategoriesController(ISender sender) : ControllerBase
{
    [HttpGet("", Name = "ListInventoryCategories")]
    [EndpointSummary("List inventory categories")]
    [EndpointDescription("List inventory categories for the authenticated tenant. Category metadata never changes ledger facts.")]
    [ProducesResponseType<IReadOnlyList<InventoryCategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<InventoryCategoryDto>>> ListInventoryCategories(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetInventoryCategoriesQuery(), cancellationToken));
    }

    [HttpPost("", Name = "CreateInventoryCategory")]
    [EndpointSummary("Create inventory category")]
    [EndpointDescription("Create inventory category for the authenticated tenant. Category metadata never changes ledger facts.")]
    [ProducesResponseType<InventoryCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryCategoryDto>> CreateInventoryCategory(CreateInventoryCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new CreateInventoryCategoryCommand(request.Code, request.Name, request.Description, request.DisplayOrder), cancellationToken));
    }

    [HttpPut("{categoryId:guid}", Name = "UpdateInventoryCategory")]
    [EndpointSummary("Update inventory category")]
    [EndpointDescription("Update inventory category for the authenticated tenant. Category metadata never changes ledger facts.")]
    [ProducesResponseType<InventoryCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryCategoryDto>> UpdateInventoryCategory(Guid categoryId, UpdateInventoryCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateInventoryCategoryCommand(categoryId, request.Name, request.Description, request.DisplayOrder, request.ExpectedVersion), cancellationToken));
    }

    [HttpPost("{categoryId:guid}/active", Name = "SetInventoryCategoryActive")]
    [EndpointSummary("Activate or deactivate inventory category")]
    [EndpointDescription("Activate or deactivate inventory category for the authenticated tenant. Category metadata never changes ledger facts.")]
    [ProducesResponseType<InventoryCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryCategoryDto>> SetInventoryCategoryActive(Guid categoryId, SetInventoryCategoryActiveRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new SetInventoryCategoryActiveCommand(categoryId, request.Active, request.ExpectedVersion), cancellationToken));
    }

}
