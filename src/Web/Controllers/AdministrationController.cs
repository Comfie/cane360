using Cane360.Application.Administration;
using Cane360.Application.Inventory;
using Cane360.Web.Models.Administration;
using System.Text;
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
        CancellationToken cancellationToken) =>
        Ok(await administration.OverviewAsync(cancellationToken));

    [HttpGet("manager-access")]
    public async Task<ActionResult<AdministrationManagerAccessDto>> ManagerAccess(
        CancellationToken cancellationToken) =>
        Ok(await administration.ManagerAccessAsync(cancellationToken));

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<AdministrationRuleDto>>> Rules(
        CancellationToken cancellationToken) =>
        Ok(await administration.RulesAsync(cancellationToken));

    [HttpPost("rules/{ruleId:guid}/end")]
    public async Task<ActionResult<AdministrationRuleDto>> EndRule(Guid ruleId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.EndRuleAsync(ruleId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("rule-items")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> RuleItems(
        CancellationToken cancellationToken) =>
        Ok(await administration.RuleItemsAsync(cancellationToken));

    [HttpGet("settings")]
    public async Task<ActionResult<IReadOnlyList<FarmSettingDto>>> Settings(
        CancellationToken cancellationToken) =>
        Ok(await administration.SettingsAsync(cancellationToken));

    [HttpPost("settings")]
    public async Task<ActionResult<FarmSettingDto>> CreateSetting(
        CreateFarmSettingRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.CreateSettingAsync(request.Key, request.Value,
            request.EffectiveFrom, request.EffectiveTo, cancellationToken));

    [HttpPost("settings/{settingId:guid}/end")]
    public async Task<ActionResult<FarmSettingDto>> EndSetting(Guid settingId,
        EndEffectiveRuleRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.EndSettingAsync(settingId, request.EffectiveTo,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("document-categories")]
    public async Task<ActionResult<IReadOnlyList<DocumentCategoryDto>>> Categories(
        CancellationToken cancellationToken) =>
        Ok(await administration.CategoriesAsync(cancellationToken));

    [HttpPost("document-categories")]
    public async Task<ActionResult<DocumentCategoryDto>> CreateCategory(
        CreateDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.CreateCategoryAsync(request.Code, request.Name,
            request.Description, cancellationToken));

    [HttpPut("document-categories/{categoryId:guid}")]
    public async Task<ActionResult<DocumentCategoryDto>> UpdateCategory(Guid categoryId,
        UpdateDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.UpdateCategoryAsync(categoryId, request.Name,
            request.Description, request.ExpectedVersion, cancellationToken));

    [HttpPost("document-categories/{categoryId:guid}/archive")]
    public async Task<ActionResult<DocumentCategoryDto>> ArchiveCategory(Guid categoryId,
        ArchiveDocumentCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.ArchiveCategoryAsync(categoryId,
            request.ExpectedVersion, cancellationToken));

    [HttpGet("session")]
    public async Task<ActionResult<AdministrationSessionDto>> Session(
        CancellationToken cancellationToken) =>
        Ok(await administration.SessionAsync(cancellationToken));

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<AdministrationCapabilityDto>>> Roles(
        CancellationToken cancellationToken) =>
        Ok(await administration.RolesAsync(cancellationToken));

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdministrationUserDto>>> Users(
        CancellationToken cancellationToken) =>
        Ok(await administration.UsersAsync(cancellationToken));

    [HttpPost("users/{membershipId:guid}/disable")]
    public async Task<IActionResult> DisableManager(Guid membershipId,
        CancellationToken cancellationToken)
    {
        await administration.DisableManagerAsync(membershipId, cancellationToken);
        return NoContent();
    }

    [HttpGet("audit")]
    public async Task<ActionResult<AdministrationAuditPageDto>> Audit(
        [FromQuery] AdministrationAuditRequest request, CancellationToken cancellationToken) =>
        Ok(await administration.AuditAsync(ToFilter(request), cancellationToken));

    [HttpGet("audit/{eventId:guid}")]
    public async Task<ActionResult<AdministrationAuditDto>> AuditDetail(
        Guid eventId, CancellationToken cancellationToken) =>
        Ok(await administration.AuditDetailAsync(eventId, cancellationToken));

    [HttpGet("audit.csv")]
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
