using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Models;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Application.Inventory;
using System.Text;

namespace Cane360.Application.Administration;

public sealed class AdministrationService(
    IFarmSetupRepository farms, IAdministrationReadRepository repository,
    IInventoryRepository inventory, IUser user,
    TimeProvider clock)
{
    public async Task<AdministrationOverviewDto> OverviewAsync(CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        IReadOnlyList<AdministrationUserDto> users = await repository.GetUsersAsync(
            tenant.Id, farm.Id, cancellationToken);
        var units = await inventory.GetUnitsAsync(tenant.Id, false, cancellationToken);
        var rules = await inventory.GetRulesAsync(tenant.Id, farm.Id, cancellationToken);
        var invitations = await inventory.GetManagerInvitationsAsync(tenant.Id, farm.Id,
            false, cancellationToken);
        AdministrationAuditPageDto? events = membership.SecurityRole == TenantSecurityRoles.Grower
            ? await repository.GetAuditAsync(tenant.Id, farm.Id,
                new AdministrationAuditFilter(null, null, null, null, null, null, null, 1, 5),
                cancellationToken)
            : null;
        AdministrationUserDto? manager = users.SingleOrDefault(item =>
            item.Role == TenantSecurityRoles.FarmManager && item.Status == nameof(RecordStatus.Active));
        return new AdministrationOverviewDto(
            users.Count(item => item.Status == nameof(RecordStatus.Active)),
            invitations.Count(item => item.RevokedAt is null && item.RedeemedAt is null &&
                item.ExpiresAt > clock.GetUtcNow()),
            manager?.PersonName ?? manager?.Email,
            tenant.ActivityTypes.Count(item => item.Status == RecordStatus.Active),
            units.Count(item => item.Status == InventoryRecordStatus.Active),
            rules.Count,
            manager is null ? 1 : 0,
            events?.Items ?? []);
    }

    public async Task<AdministrationManagerAccessDto> ManagerAccessAsync(
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(true, cancellationToken);
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare")).DateTime);
        var managerCandidates = farm.Persons
            .Where(person => person.Status == RecordStatus.Active &&
                person.RoleAssignments.Any(role => role.Role == PersonRole.FarmManager &&
                    role.IsPrimary && role.IsEffective(today)))
            .Select(person => new AdministrationManagerCandidateDto(person.Id, person.DisplayName,
                TenantSecurityRoles.FarmManager));
        var supervisorCandidates = farm.Persons
            .Where(person => person.Status == RecordStatus.Active &&
                person.RoleAssignments.Any(role => role.Role == PersonRole.Supervisor &&
                    role.IsEffective(today)))
            .Select(person => new AdministrationManagerCandidateDto(person.Id, person.DisplayName,
                TenantSecurityRoles.Supervisor));
        AdministrationManagerCandidateDto[] candidates = managerCandidates.Concat(supervisorCandidates).ToArray();
        var invitations = await inventory.GetManagerInvitationsAsync(tenant.Id, farm.Id,
            false, cancellationToken);
        return new AdministrationManagerAccessDto(candidates,
            invitations.Select(item => new ManagerInvitationDto(item.Id, item.PersonId,
                item.ExpiresAt, item.RevokedAt, item.RedeemedAt, item.Version, item.SecurityRole)).ToArray());
    }

    public async Task<IReadOnlyList<AdministrationRuleDto>> RulesAsync(
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(false, cancellationToken);
        var rules = await inventory.GetRulesAsync(tenant.Id, farm.Id, cancellationToken);
        var items = await inventory.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        Dictionary<Guid, string> itemNames = items.ToDictionary(item => item.Id, item => item.Name);
        Dictionary<Guid, string> activityNames = tenant.ActivityTypes.ToDictionary(
            item => item.Id, item => item.Name);
        return rules.OrderByDescending(item => item.EffectiveFrom)
            .Select(item => new AdministrationRuleDto(item.Id, item.InventoryItemId,
                itemNames.GetValueOrDefault(item.InventoryItemId, "Historical item"),
                item.ActivityTypeId,
                activityNames.GetValueOrDefault(item.ActivityTypeId, "Historical activity"),
                item.UnitCodeSnapshot, item.CoverageBasis.ToString(),
                item.RatePerCoverageUnit, item.LowerTolerancePercent,
                item.UpperTolerancePercent, item.EffectiveFrom, item.EffectiveTo,
                item.Version)).ToArray();
    }

    public async Task<AdministrationRuleDto> EndRuleAsync(Guid ruleId,
        DateOnly effectiveTo, long expectedVersion, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(false, cancellationToken);
        var rule = await inventory.GetRuleAsync(tenant.Id, farm.Id, ruleId, true,
            cancellationToken) ?? throw new NotFoundException(ruleId.ToString(),
            "Inventory application rule");
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare")).DateTime);
        if (effectiveTo < today)
            throw new ConflictException("Application rules cannot be ended retroactively.");
        try { rule.End(effectiveTo, expectedVersion); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        InventoryAudit.Rule(inventory, tenant, farm, user, rule, "Ended",
            clock.GetUtcNow(), "Effective-dated inventory application rule ended; historical application snapshots remain unchanged.");
        await inventory.SaveChangesAsync(cancellationToken);
        var item = await inventory.GetItemAsync(tenant.Id, farm.Id, rule.InventoryItemId,
            false, cancellationToken);
        var type = tenant.ActivityTypes.SingleOrDefault(candidate => candidate.Id == rule.ActivityTypeId);
        return new AdministrationRuleDto(rule.Id, rule.InventoryItemId,
            item?.Name ?? "Historical item", rule.ActivityTypeId,
            type?.Name ?? "Historical activity", rule.UnitCodeSnapshot,
            rule.CoverageBasis.ToString(), rule.RatePerCoverageUnit,
            rule.LowerTolerancePercent, rule.UpperTolerancePercent,
            rule.EffectiveFrom, rule.EffectiveTo, rule.Version);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> RuleItemsAsync(
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(false, cancellationToken);
        var items = await inventory.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        return items.Where(item => item.Status == InventoryRecordStatus.Active)
            .OrderBy(item => item.Code).Select(InventoryMapper.Item).ToArray();
    }

    public async Task<IReadOnlyList<FarmSettingDto>> SettingsAsync(CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(false, cancellationToken);
        var settings = await farms.GetFarmSettingsAsync(tenant.Id, farm.Id, false, cancellationToken);
        return settings.Select(MapSetting).ToArray();
    }

    public async Task<IReadOnlyList<DocumentCategoryDto>> CategoriesAsync(
        CancellationToken cancellationToken)
    {
        (Tenant tenant, _, _) = await ContextAsync(false, cancellationToken);
        var categories = await repository.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        return categories.Select(MapCategory).ToArray();
    }

    public async Task<DocumentCategoryDto> CreateCategoryAsync(string code, string name,
        string? description, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        string normalizedCode = code.Trim().ToUpperInvariant();
        var existing = await repository.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        if (existing.Any(item => item.Code == normalizedCode))
            throw new ConflictException("Document category code already exists in this tenant.");
        DocumentCategory category = DocumentCategory.Create(tenant.Id, code, name, description);
        repository.Add(category);
        RecordCategoryAudit(tenant, farm, membership, category, "Created");
        await repository.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<DocumentCategoryDto> UpdateCategoryAsync(Guid categoryId,
        string name, string? description, long expectedVersion,
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        DocumentCategory category = await repository.GetCategoryAsync(tenant.Id, categoryId,
            true, cancellationToken) ?? throw new NotFoundException(categoryId.ToString(),
            "Document category");
        try { category.Update(name, description, expectedVersion); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        RecordCategoryAudit(tenant, farm, membership, category, "Updated");
        await repository.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<DocumentCategoryDto> ArchiveCategoryAsync(Guid categoryId,
        long expectedVersion, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        DocumentCategory category = await repository.GetCategoryAsync(tenant.Id, categoryId,
            true, cancellationToken) ?? throw new NotFoundException(categoryId.ToString(),
            "Document category");
        try { category.Archive(expectedVersion); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        RecordCategoryAudit(tenant, farm, membership, category, "Archived");
        await repository.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    private void RecordCategoryAudit(Tenant tenant, Farm farm, TenantMembership membership,
        DocumentCategory category, string action) =>
        inventory.Add(AuditEvent.Create(tenant.Id, farm.Id, "DocumentCategory", category.Id,
            action, membership.UserId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            $"Document category {category.Code} {action.ToLowerInvariant()}."));

    private static DocumentCategoryDto MapCategory(DocumentCategory category) => new(
        category.Id, category.Code, category.Name, category.Description,
        category.Active, category.Version);

    public async Task<FarmSettingDto> CreateSettingAsync(string key, int value,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare")).DateTime);
        if (effectiveFrom < today)
            throw new ConflictException("Farm setting versions cannot start retroactively.");
        var existing = await farms.GetFarmSettingsAsync(tenant.Id, farm.Id, false, cancellationToken);
        if (existing.Any(item => item.Key == key &&
            effectiveFrom <= (item.EffectiveTo ?? DateOnly.MaxValue) &&
            item.EffectiveFrom <= (effectiveTo ?? DateOnly.MaxValue)))
            throw new ConflictException("This setting overlaps an existing effective version.");
        FarmSetting setting;
        try { setting = FarmSetting.Create(tenant.Id, farm.Id, key, value, effectiveFrom, effectiveTo); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        farms.Add(setting);
        inventory.Add(AuditEvent.Create(tenant.Id, farm.Id, "FarmSetting", setting.Id,
            "Created", membership.UserId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            "Effective-dated activity late-entry threshold configured."));
        await farms.SaveChangesAsync(cancellationToken);
        return MapSetting(setting);
    }

    public async Task<FarmSettingDto> EndSettingAsync(Guid settingId, DateOnly effectiveTo,
        long expectedVersion, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        var settings = await farms.GetFarmSettingsAsync(tenant.Id, farm.Id, true, cancellationToken);
        FarmSetting setting = settings.SingleOrDefault(item => item.Id == settingId)
            ?? throw new NotFoundException(settingId.ToString(), "Farm setting");
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare")).DateTime);
        if (effectiveTo < today)
            throw new ConflictException("Farm settings cannot be ended retroactively.");
        try { setting.End(effectiveTo, expectedVersion); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        farms.Add(AuditEvent.Create(tenant.Id, farm.Id, "FarmSetting", setting.Id,
            "Ended", membership.UserId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            "Effective-dated activity late-entry threshold ended; historical work is unchanged."));
        await farms.SaveChangesAsync(cancellationToken);
        return MapSetting(setting);
    }

    private static FarmSettingDto MapSetting(FarmSetting setting) => new(setting.Id,
        setting.Key, "Activity late-entry reason after days", setting.Value,
        setting.EffectiveFrom, setting.EffectiveTo, setting.Version);

    public async Task<AdministrationSessionDto> SessionAsync(CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(false, cancellationToken);
        return new AdministrationSessionDto(membership.SecurityRole, tenant.TenantCode, farm.Name);
    }

    public async Task<IReadOnlyList<AdministrationCapabilityDto>> RolesAsync(
        CancellationToken cancellationToken)
    {
        await ContextAsync(false, cancellationToken);
        return AdministrationCapabilities.Matrix();
    }

    public async Task<AdministrationUserDto> DisableManagerAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        Tenant tenant = await farms.GetTenantAdministrationContextForUserAsync(userId, true, cancellationToken)
            ?? throw new NotFoundException(userId, "Active tenant membership");
        Farm farm = tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");
        TenantMembership actor = tenant.Memberships.Single(item => item.UserId == userId &&
            item.Status == RecordStatus.Active);
        if (!AdministrationCapabilities.Can(actor.SecurityRole,
            AdministrationCapabilities.UserAdministration))
            throw new ForbiddenAccessException();
        TenantMembership target = tenant.Memberships.SingleOrDefault(item => item.Id == membershipId)
            ?? throw new NotFoundException(membershipId.ToString(), "Tenant membership");
        try { tenant.DisableMembership(target.Id); }
        catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        inventory.Add(AuditEvent.Create(tenant.Id, farm.Id, "TenantMembership", target.Id,
            "Disabled", userId, actor.SecurityRole, actor.PersonId, clock.GetUtcNow(),
            user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            $"Grower disabled a {target.SecurityRole} application membership."));
        await farms.SaveChangesAsync(cancellationToken);
        return (await repository.GetUsersAsync(tenant.Id, farm.Id, cancellationToken))
            .Single(item => item.MembershipId == target.Id);
    }

    public async Task<IReadOnlyList<AdministrationUserDto>> UsersAsync(
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(true, cancellationToken);
        return await repository.GetUsersAsync(tenant.Id, farm.Id, cancellationToken);
    }

    public async Task<AdministrationAuditPageDto> AuditAsync(
        AdministrationAuditFilter filter, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(true, cancellationToken);
        if (filter.Page < 1 || filter.PageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(filter), "Page must be positive and page size must be between 1 and 100.");
        if (filter.From.HasValue && filter.To.HasValue && filter.To < filter.From)
            throw new ArgumentException("Audit end time must follow the start time.", nameof(filter));
        return await repository.GetAuditAsync(tenant.Id, farm.Id, filter, cancellationToken);
    }

    public async Task<AdministrationAuditDto> AuditDetailAsync(
        Guid eventId, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await ContextAsync(true, cancellationToken);
        return await repository.GetAuditEventAsync(tenant.Id, farm.Id, eventId, cancellationToken)
            ?? throw new NotFoundException(eventId.ToString(), "Audit event");
    }

    public async Task<AdministrationAuditExportDto> ExportAuditAsync(
        AdministrationAuditFilter filter, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, TenantMembership membership) =
            await ContextAsync(true, cancellationToken);
        if (filter.From.HasValue && filter.To.HasValue && filter.To < filter.From)
            throw new ArgumentException("Audit end time must follow the start time.", nameof(filter));

        var rows = new List<AdministrationAuditDto>();
        for (int page = 1; ; page++)
        {
            AdministrationAuditPageDto batch = await repository.GetAuditAsync(tenant.Id, farm.Id,
                filter with { Page = page, PageSize = 100 }, cancellationToken);
            if (batch.TotalCount > 10000)
                throw new InvalidOperationException("Narrow the audit filters to export at most 10,000 events.");
            rows.AddRange(batch.Items);
            if (rows.Count >= batch.TotalCount) break;
        }

        DateTimeOffset generatedAt = clock.GetUtcNow();
        var csv = new StringBuilder();
        csv.AppendLine("Tenant,Farm,GeneratedAt,From,To,Action,SubjectType,AuthenticatedUserId,OperationalPersonId,CorrelationId");
        csv.AppendJoin(',', CsvCell.Text(tenant.TenantCode), CsvCell.Text(farm.Code),
            CsvCell.Text(generatedAt.ToString("O")), CsvCell.Text(filter.From?.ToString("O")),
            CsvCell.Text(filter.To?.ToString("O")), CsvCell.Text(filter.Action),
            CsvCell.Text(filter.SubjectType), CsvCell.Text(filter.AuthenticatedUserId),
            CsvCell.Text(filter.OperationalPersonId?.ToString()), CsvCell.Text(filter.CorrelationId));
        csv.AppendLine();
        csv.AppendLine("OccurredAt,Action,SubjectType,SubjectId,AuthenticatedUser,OperationalPerson,SafeSummary,Reason,CorrelationId");
        foreach (AdministrationAuditDto row in rows)
        {
            csv.AppendJoin(',', CsvCell.Text(row.OccurredAt.ToString("O")),
                CsvCell.Text(row.Action), CsvCell.Text(row.SubjectType),
                CsvCell.Text(row.SubjectId.ToString()),
                CsvCell.Text(row.AuthenticatedUserEmail ?? row.AuthenticatedUserId),
                CsvCell.Text(row.OperationalPersonName), CsvCell.Text(row.SafeSummary),
                CsvCell.Text(row.Reason), CsvCell.Text(row.CorrelationId));
            csv.AppendLine();
        }

        AuditEvent fact = AuditEvent.Create(tenant.Id, farm.Id, "AdministrationAuditExport",
            Guid.NewGuid(), "Exported", membership.UserId, membership.SecurityRole,
            membership.PersonId, generatedAt, user.CorrelationId ?? Guid.NewGuid().ToString("N"),
            null, $"Tenant audit export generated with {rows.Count} events.");
        await repository.RecordExportAsync(fact, cancellationToken);
        return new AdministrationAuditExportDto(
            $"cane360-audit-{generatedAt:yyyyMMdd-HHmmss}.csv", csv.ToString());
    }

    private async Task<(Tenant Tenant, Farm Farm, TenantMembership Membership)> ContextAsync(
        bool requireGrower, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        Tenant tenant = await farms.GetTenantAdministrationContextForUserAsync(userId, false, cancellationToken)
            ?? throw new NotFoundException(userId, "Active tenant membership");
        TenantMembership membership = tenant.Memberships.Single(item =>
            item.UserId == userId && item.Status == RecordStatus.Active);
        if (requireGrower && !AdministrationCapabilities.Can(membership.SecurityRole,
            AdministrationCapabilities.AuditAccess))
            throw new ForbiddenAccessException();
        Farm farm = tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");
        return (tenant, farm, membership);
    }
}
