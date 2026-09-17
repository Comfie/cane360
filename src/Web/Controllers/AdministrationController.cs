using System.Text;
using Cane360.Application.Administration;
using Cane360.Application.Inventory;
using Cane360.Web.Models.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/administration")]
public sealed class AdministrationController(AdministrationService administration) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<AdministrationOverviewDto>> Overview(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.OverviewAsync(cancellationToken));
    }

    [HttpGet("manager-access")]
    public async Task<ActionResult<AdministrationManagerAccessDto>> ManagerAccess(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.ManagerAccessAsync(cancellationToken));
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<AdministrationRuleDto>>> Rules(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.RulesAsync(cancellationToken));
    }

    [HttpPost("rules/{ruleId:guid}/end")]
    public async Task<ActionResult<AdministrationRuleDto>> EndRule(Guid ruleId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.EndRuleAsync(ruleId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));
    }

    [HttpGet("rule-items")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> RuleItems(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.RuleItemsAsync(cancellationToken));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<IReadOnlyList<FarmSettingDto>>> Settings(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.SettingsAsync(cancellationToken));
    }

    [HttpPost("settings")]
    public async Task<ActionResult<FarmSettingDto>> CreateSetting(
        CreateFarmSettingRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.CreateSettingAsync(request.Key, request.Value,
            request.EffectiveFrom, request.EffectiveTo, cancellationToken));
    }

    [HttpPost("settings/{settingId:guid}/end")]
    public async Task<ActionResult<FarmSettingDto>> EndSetting(Guid settingId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.EndSettingAsync(settingId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));
    }

    [HttpGet("document-categories")]
    public async Task<ActionResult<IReadOnlyList<DocumentCategoryDto>>> Categories(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.CategoriesAsync(cancellationToken));
    }

    [HttpPost("document-categories")]
    public async Task<ActionResult<DocumentCategoryDto>> CreateCategory(
        CreateDocumentCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.CreateCategoryAsync(request.Code, request.Name,
            request.Description, cancellationToken));
    }

    [HttpPut("document-categories/{categoryId:guid}")]
    public async Task<ActionResult<DocumentCategoryDto>> UpdateCategory(Guid categoryId,
        UpdateDocumentCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.UpdateCategoryAsync(categoryId, request.Name,
            request.Description, request.ExpectedVersion, cancellationToken));
    }

    [HttpPost("document-categories/{categoryId:guid}/archive")]
    public async Task<ActionResult<DocumentCategoryDto>> ArchiveCategory(Guid categoryId,
        ArchiveDocumentCategoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.ArchiveCategoryAsync(categoryId,
            request.ExpectedVersion, cancellationToken));
    }

    [HttpGet("session")]
    public async Task<ActionResult<AdministrationSessionDto>> Session(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.SessionAsync(cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<AdministrationCapabilityDto>>> Roles(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.RolesAsync(cancellationToken));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdministrationUserDto>>> Users(
        CancellationToken cancellationToken)
    {
        return Ok(await administration.UsersAsync(cancellationToken));
    }

    [HttpPost("users/{membershipId:guid}/disable")]
    public async Task<IActionResult> DisableManager(Guid membershipId,
        CancellationToken cancellationToken)
    {
        await administration.DisableManagerAsync(membershipId, cancellationToken);
        return NoContent();
    }

    [HttpGet("audit")]
    public async Task<ActionResult<AdministrationAuditPageDto>> Audit(
        [FromQuery] AdministrationAuditRequest request, CancellationToken cancellationToken)
    {
        return Ok(await administration.AuditAsync(ToFilter(request), cancellationToken));
    }

    [HttpGet("audit/{eventId:guid}")]
    public async Task<ActionResult<AdministrationAuditDto>> AuditDetail(
        Guid eventId, CancellationToken cancellationToken)
    {
        return Ok(await administration.AuditDetailAsync(eventId, cancellationToken));
    }

    [HttpGet("audit.csv")]
    public async Task<IActionResult> ExportAudit([FromQuery] AdministrationAuditRequest request,
        CancellationToken cancellationToken)
    {
        AdministrationAuditExportDto export = await administration.ExportAuditAsync(
            ToFilter(request), cancellationToken);
        return File(Encoding.UTF8.GetBytes(export.Content), "text/csv; charset=utf-8",
            export.FileName);
    }

    private static AdministrationAuditFilter ToFilter(AdministrationAuditRequest request)
    {
        return new AdministrationAuditFilter(
            request.From, request.To, request.Action, request.SubjectType,
            request.AuthenticatedUserId, request.OperationalPersonId, request.CorrelationId,
            request.Page, request.PageSize);
    }
}
