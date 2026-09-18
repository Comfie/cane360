using Cane360.Application.Administration;
using Cane360.Application.Inventory;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Administration;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/administration")]
public sealed class AdministrationController(AdministrationService administration) : ControllerBase
{
    [HttpGet("overview")]
    [EndpointSummary("Get administration overview")]
    [EndpointDescription("Returns administration overview for the authenticated farm.")]
    [ProducesResponseType<AdministrationOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdministrationOverviewDto>> Overview(
        CancellationToken cancellationToken) =>
        Ok(await administration.OverviewAsync(cancellationToken));

    [HttpGet("manager-access")]
    [EndpointSummary("Get manager access")]
    [EndpointDescription("Returns manager access for the authenticated farm.")]
    [ProducesResponseType<AdministrationManagerAccessDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdministrationManagerAccessDto>> ManagerAccess(
        CancellationToken cancellationToken) =>
        Ok(await administration.ManagerAccessAsync(cancellationToken));

    [HttpGet("rules", Name = "GetRulesAdministration")]
    [EndpointSummary("List application rules")]
    [EndpointDescription("Returns application rules for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<AdministrationRuleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AdministrationRuleDto>>> Rules(
        CancellationToken cancellationToken) =>
        Ok(await administration.RulesAsync(cancellationToken));

    [HttpPost("rules/{ruleId:guid}/end", Name = "EndRuleAdministration")]
    [EndpointSummary("End application rule")]
    [EndpointDescription("Ends application rule for the authenticated farm.")]
    [ProducesResponseType<AdministrationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdministrationRuleDto>> EndRule(Guid ruleId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.EndRuleAsync(ruleId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("rule-items")]
    [EndpointSummary("List rule inventory items")]
    [EndpointDescription("Returns rule inventory items for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<InventoryItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> RuleItems(
        CancellationToken cancellationToken) =>
        Ok(await administration.RuleItemsAsync(cancellationToken));

    [HttpGet("settings", Name = "GetSettingsAdministration")]
    [EndpointSummary("List farm settings")]
    [EndpointDescription("Returns farm settings for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<FarmSettingDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FarmSettingDto>>> Settings(
        CancellationToken cancellationToken) =>
        Ok(await administration.SettingsAsync(cancellationToken));

    [HttpPost("settings", Name = "CreateSettingAdministration")]
    [EndpointSummary("Create farm setting")]
    [EndpointDescription("Creates farm setting for the authenticated farm.")]
    [ProducesResponseType<FarmSettingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FarmSettingDto>> CreateSetting(
        CreateFarmSettingRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.CreateSettingAsync(request.Key, request.Value,
            request.EffectiveFrom, request.EffectiveTo, cancellationToken));

    [HttpPost("settings/{settingId:guid}/end", Name = "EndSettingAdministration")]
    [EndpointSummary("End farm setting")]
    [EndpointDescription("Ends farm setting for the authenticated farm.")]
    [ProducesResponseType<FarmSettingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FarmSettingDto>> EndSetting(Guid settingId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.EndSettingAsync(settingId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("document-categories", Name = "GetCategoriesAdministration")]
    [EndpointSummary("List document categories")]
    [EndpointDescription("Returns document categories for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<DocumentCategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DocumentCategoryDto>>> Categories(
        CancellationToken cancellationToken) =>
        Ok(await administration.CategoriesAsync(cancellationToken));

    [HttpPost("document-categories", Name = "CreateCategoryAdministration")]
    [EndpointSummary("Create document category")]
    [EndpointDescription("Creates document category for the authenticated farm.")]
    [ProducesResponseType<DocumentCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentCategoryDto>> CreateCategory(
        CreateDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.CreateCategoryAsync(request.Code, request.Name,
            request.Description, cancellationToken));

    [HttpPut("document-categories/{categoryId:guid}")]
    [EndpointSummary("Update document category")]
    [EndpointDescription("Updates document category for the authenticated farm.")]
    [ProducesResponseType<DocumentCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentCategoryDto>> UpdateCategory(Guid categoryId,
        UpdateDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.UpdateCategoryAsync(categoryId, request.Name,
            request.Description, request.ExpectedVersion, cancellationToken));

    [HttpPost("document-categories/{categoryId:guid}/archive", Name = "ArchiveCategoryAdministration")]
    [EndpointSummary("Archive document category")]
    [EndpointDescription("Archives document category for the authenticated farm.")]
    [ProducesResponseType<DocumentCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentCategoryDto>> ArchiveCategory(Guid categoryId,
        ArchiveDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.ArchiveCategoryAsync(categoryId,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("session", Name = "GetSessionAdministration")]
    [EndpointSummary("Get administration session")]
    [EndpointDescription("Returns administration session for the authenticated farm.")]
    [ProducesResponseType<AdministrationSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdministrationSessionDto>> Session(
        CancellationToken cancellationToken) =>
        Ok(await administration.SessionAsync(cancellationToken));

    [HttpGet("roles")]
    [EndpointSummary("List administration capabilities")]
    [EndpointDescription("Returns administration capabilities for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<AdministrationCapabilityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AdministrationCapabilityDto>>> Roles(
        CancellationToken cancellationToken) =>
        Ok(await administration.RolesAsync(cancellationToken));

    [HttpGet("users")]
    [EndpointSummary("List administration users")]
    [EndpointDescription("Returns administration users for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<AdministrationUserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AdministrationUserDto>>> Users(
        CancellationToken cancellationToken) =>
        Ok(await administration.UsersAsync(cancellationToken));

    [HttpPost("users/{membershipId:guid}/disable")]
    [EndpointSummary("Disable manager access")]
    [EndpointDescription("Disables manager access for the authenticated farm.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DisableManager(Guid membershipId,
        CancellationToken cancellationToken)
    {
        await administration.DisableManagerAsync(membershipId, cancellationToken);
        return NoContent();
    }

    [HttpGet("audit")]
    [EndpointSummary("Get audit events")]
    [EndpointDescription("Returns audit events for the authenticated farm.")]
    [ProducesResponseType<AdministrationAuditPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdministrationAuditPageDto>> Audit(
        [FromQuery] AdministrationAuditRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.AuditAsync(ToFilter(request), cancellationToken));

    [HttpGet("audit/{eventId:guid}", Name = "GetAdministrationAuditDetail")]
    [EndpointSummary("Get audit event detail")]
    [EndpointDescription("Returns audit event detail for the authenticated farm.")]
    [ProducesResponseType<AdministrationAuditDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdministrationAuditDto>> AuditDetail(
        Guid eventId, CancellationToken cancellationToken) =>
        Ok(await administration.AuditDetailAsync(eventId, cancellationToken));

    [HttpGet("audit.csv")]
    [EndpointSummary("Export audit events")]
    [EndpointDescription("Exports audit events for the authenticated farm.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EnableRateLimiting(ApiRateLimitOptions.ExportsPolicy)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ExportAudit([FromQuery] AdministrationAuditRequest request,
        CancellationToken cancellationToken)
    {
        AdministrationAuditExportDto export = await administration.ExportAuditAsync(
            ToFilter(request), cancellationToken);
        return File(Encoding.UTF8.GetBytes(export.Content), "text/csv; charset=utf-8",
            export.FileName);
    }

    private static AdministrationAuditFilter ToFilter(AdministrationAuditRequest request) => new(
        request.From, request.To, request.Action, request.SubjectType,
        request.AuthenticatedUserId, request.OperationalPersonId, request.CorrelationId,
        request.Page, request.PageSize);
}
