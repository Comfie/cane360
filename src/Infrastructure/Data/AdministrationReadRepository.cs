using Cane360.Application.Administration;
using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Infrastructure.Data;

public sealed class AdministrationReadRepository(ApplicationDbContext context) : IAdministrationReadRepository
{
    public async Task<IReadOnlyList<AdministrationUserDto>> GetUsersAsync(
        Guid tenantId, Guid farmId, CancellationToken cancellationToken)
    {
        var rows = await context.TenantMemberships.AsNoTracking()
            .Where(membership => membership.TenantId == tenantId)
            .OrderBy(membership => membership.SecurityRole)
            .ThenBy(membership => membership.UserId)
            .Select(membership => new
            {
                membership.Id,
                membership.UserId,
                membership.SecurityRole,
                membership.Status,
                membership.PersonId,
                Email = context.Users.Where(user => user.Id == membership.UserId)
                    .Select(user => user.Email).FirstOrDefault(),
                PersonName = context.Persons.Where(person => person.Id == membership.PersonId &&
                    person.FarmId == farmId)
                    .Select(person => person.DisplayName).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return rows.Select(row => new AdministrationUserDto(row.Id, row.UserId, row.Email,
            row.SecurityRole, row.Status.ToString(), row.PersonId, row.PersonName)).ToArray();
    }

    public async Task<AdministrationAuditPageDto> GetAuditAsync(
        Guid tenantId, Guid farmId, AdministrationAuditFilter filter,
        CancellationToken cancellationToken)
    {
        IQueryable<AuditEvent> query = Filter(tenantId, farmId, filter);
        int count = await query.CountAsync(cancellationToken);
        var events = await query.OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new AdministrationAuditPageDto(filter.Page, filter.PageSize, count,
            await MapAsync(events, tenantId, farmId, cancellationToken));
    }

    public async Task<AdministrationAuditDto?> GetAuditEventAsync(
        Guid tenantId, Guid farmId, Guid eventId, CancellationToken cancellationToken)
    {
        AuditEvent? audit = await context.AuditEvents.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == eventId && item.TenantId == tenantId && item.FarmId == farmId,
            cancellationToken);
        if (audit is null) return null;
        return (await MapAsync([audit], tenantId, farmId, cancellationToken))[0];
    }

    public async Task RecordExportAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentCategory>> GetCategoriesAsync(Guid tenantId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<DocumentCategory> query = context.DocumentCategories
            .Where(item => item.TenantId == tenantId);
        return await (trackChanges ? query : query.AsNoTracking())
            .OrderBy(item => item.Code).ToListAsync(cancellationToken);
    }

    public Task<DocumentCategory?> GetCategoryAsync(Guid tenantId, Guid categoryId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<DocumentCategory> query = context.DocumentCategories
            .Where(item => item.TenantId == tenantId && item.Id == categoryId);
        return (trackChanges ? query : query.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(DocumentCategory category) => context.DocumentCategories.Add(category);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This document category changed after it was loaded.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "IX_DocumentCategories_TenantId_Code" })
        {
            throw new ConflictException("Document category code already exists in this tenant.");
        }
    }

    private IQueryable<AuditEvent> Filter(Guid tenantId, Guid farmId,
        AdministrationAuditFilter filter)
    {
        IQueryable<AuditEvent> query = context.AuditEvents.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.FarmId == farmId);
        if (filter.From.HasValue) query = query.Where(item => item.OccurredAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(item => item.OccurredAt <= filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(item => item.Action == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.SubjectType))
            query = query.Where(item => item.SubjectType == filter.SubjectType);
        if (!string.IsNullOrWhiteSpace(filter.AuthenticatedUserId))
            query = query.Where(item => item.AuthenticatedUserId == filter.AuthenticatedUserId);
        if (filter.OperationalPersonId.HasValue)
            query = query.Where(item => item.OperationalPersonId == filter.OperationalPersonId.Value);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
            query = query.Where(item => item.CorrelationId == filter.CorrelationId);
        return query;
    }

    private async Task<IReadOnlyList<AdministrationAuditDto>> MapAsync(
        IReadOnlyList<AuditEvent> events, Guid tenantId, Guid farmId,
        CancellationToken cancellationToken)
    {
        string[] userIds = events.Select(item => item.AuthenticatedUserId).Distinct().ToArray();
        Guid[] personIds = events.Where(item => item.OperationalPersonId.HasValue)
            .Select(item => item.OperationalPersonId!.Value).Distinct().ToArray();
        var users = await context.Users.AsNoTracking().Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Email, cancellationToken);
        var people = await context.Persons.AsNoTracking().Where(item =>
            item.FarmId == farmId && personIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        return events.Select(item => new AdministrationAuditDto(item.Id, item.OccurredAt,
            item.SubjectType, item.SubjectId, item.Action, item.AuthenticatedUserId,
            users.GetValueOrDefault(item.AuthenticatedUserId), item.OperationalPersonId,
            item.OperationalPersonId.HasValue ? people.GetValueOrDefault(item.OperationalPersonId.Value) : null,
            item.SafeSummary, item.Reason, item.CorrelationId)).ToArray();
    }
}
