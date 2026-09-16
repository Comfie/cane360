using System.Data;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.MillRecords;
using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class MillRecordsRepository(ApplicationDbContext context) : IMillRecordsRepository
{
    public async Task<MillTicketPageSource> GetTicketPageAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ArgumentOutOfRangeException(nameof(page));
        return await ReadTicketSourceAsync(tenantId, farmId, filter, page, pageSize, cancellationToken);
    }

    public Task<MillTicketPageSource> GetTicketReportSourceAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, CancellationToken cancellationToken) =>
        ReadTicketSourceAsync(tenantId, farmId, filter, null, null, cancellationToken);

    public Task<MillStatementPageSource> GetStatementPageSourceAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ArgumentOutOfRangeException(nameof(page));
        return ReadStatementSourceAsync(tenantId, farmId, filter, page, pageSize, cancellationToken);
    }

    public Task<MillStatementPageSource> GetStatementReportSourceAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, CancellationToken cancellationToken) =>
        ReadStatementSourceAsync(tenantId, farmId, filter, null, null, cancellationToken);

    private async Task<MillStatementPageSource> ReadStatementSourceAsync(Guid tenantId, Guid farmId,
        StatementFilter filter, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GrowerStatement> query = StatementQuery(tenantId, farmId, filter)
            .Where(statement => !context.GrowerStatements.Any(correction => correction.TenantId == tenantId &&
                correction.FarmId == farmId && correction.CorrectsStatementId == statement.Id &&
                correction.Status == GrowerStatementStatus.Recorded));
        IQueryable<StatementTicketMatch> activeMatches = context.StatementTicketMatches.Where(match =>
            match.TenantId == tenantId && match.FarmId == farmId && match.Action == StatementTicketMatchAction.Added &&
            !context.StatementTicketMatches.Any(reversal => reversal.TenantId == tenantId &&
                reversal.FarmId == farmId && reversal.ReversesMatchId == match.Id));
        if (!string.IsNullOrWhiteSpace(filter.MatchStatus))
        {
            string status = filter.MatchStatus;
            if (status == nameof(ReconciliationStatus.Unmatched))
                query = query.Where(statement => !activeMatches.Any(match => match.GrowerStatementId == statement.Id));
            else if (status == nameof(ReconciliationStatus.PartiallyMatched))
                query = query.Where(statement => activeMatches.Any(match => match.GrowerStatementId == statement.Id) &&
                    !activeMatches.Any(match => match.GrowerStatementId == statement.Id && match.CompletesMatching));
            else if (status == nameof(ReconciliationStatus.Matched))
                query = query.Where(statement => activeMatches.Any(match => match.GrowerStatementId == statement.Id && match.CompletesMatching) &&
                    activeMatches.Where(match => match.GrowerStatementId == statement.Id).Sum(match => match.MatchedTonnes) == statement.TotalTonnes);
            else if (status == nameof(ReconciliationStatus.Variance))
                query = query.Where(statement => activeMatches.Any(match => match.GrowerStatementId == statement.Id && match.CompletesMatching) &&
                    activeMatches.Where(match => match.GrowerStatementId == statement.Id).Sum(match => match.MatchedTonnes) != statement.TotalTonnes);
        }
        int totalCount = await query.CountAsync(cancellationToken);
        IQueryable<GrowerStatement> ordered = query.OrderByDescending(x => x.PeriodEnd)
            .ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        if (page.HasValue && pageSize.HasValue)
            ordered = ordered.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        var rows = await ordered.AsSingleQuery().Select(statement => new
        {
            Statement = statement,
            Mill = context.Mills.Single(mill => mill.TenantId == tenantId && mill.FarmId == farmId &&
                mill.Id == statement.MillId),
            Evidence = context.EvidenceDocuments.Where(evidence => evidence.TenantId == tenantId &&
                evidence.FarmId == farmId && evidence.GrowerStatementId == statement.Id)
                .OrderByDescending(evidence => evidence.UploadedAt).ThenBy(evidence => evidence.Id).ToArray(),
            Matches = context.StatementTicketMatches.Where(match => match.TenantId == tenantId &&
                match.FarmId == farmId && match.GrowerStatementId == statement.Id)
                .OrderBy(match => match.CreatedAt).ThenBy(match => match.Id).ToArray()
        }).ToArrayAsync(cancellationToken);
        GrowerStatement[] statements = rows.Select(x => x.Statement).ToArray();
        Mill[] mills = rows.Select(x => x.Mill).DistinctBy(x => x.Id).ToArray();
        EvidenceDocument[] evidence = rows.SelectMany(x => x.Evidence).ToArray();
        StatementTicketMatch[] matches = rows.SelectMany(x => x.Matches).ToArray();
        Guid[] ticketIds = matches.Select(x => x.WeighbridgeTicketId).Distinct().ToArray();
        WeighbridgeTicket[] tickets = [];
        StatementTicketMatch[] related = [];
        if (ticketIds.Length != 0)
        {
            var ticketRows = await context.WeighbridgeTickets.AsNoTracking().Where(x =>
                x.TenantId == tenantId && x.FarmId == farmId && ticketIds.Contains(x.Id)).Select(ticket => new
                {
                    Ticket = ticket,
                    Related = context.StatementTicketMatches.Where(match => match.TenantId == tenantId &&
                        match.FarmId == farmId && match.WeighbridgeTicketId == ticket.Id).ToArray()
                }).ToArrayAsync(cancellationToken);
            tickets = ticketRows.Select(x => x.Ticket).ToArray();
            related = ticketRows.SelectMany(x => x.Related).ToArray();
        }
        return new(statements, mills, evidence, matches, tickets, related, totalCount);
    }

    private async Task<MillTicketPageSource> ReadTicketSourceAsync(Guid tenantId, Guid farmId,
        TicketFilter filter, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        IQueryable<WeighbridgeTicket> query = TicketQuery(tenantId, farmId, filter)
            .Where(ticket => !context.WeighbridgeTickets.Any(correction =>
                correction.TenantId == tenantId && correction.FarmId == farmId &&
                correction.CorrectsTicketId == ticket.Id && correction.Status == WeighbridgeTicketStatus.Recorded));
        IQueryable<StatementTicketMatch> activeMatches = context.StatementTicketMatches.Where(match =>
            match.TenantId == tenantId && match.FarmId == farmId && match.Action == StatementTicketMatchAction.Added &&
            !context.StatementTicketMatches.Any(reversal => reversal.TenantId == tenantId &&
                reversal.FarmId == farmId && reversal.ReversesMatchId == match.Id));
        if (!string.IsNullOrWhiteSpace(filter.MatchStatus))
        {
            bool matched = filter.MatchStatus.Equals("Matched", StringComparison.OrdinalIgnoreCase);
            query = query.Where(ticket => activeMatches.Any(match => match.WeighbridgeTicketId == ticket.Id) == matched);
        }
        var summary = await query.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Tonnes = group.Sum(ticket => ticket.Status == WeighbridgeTicketStatus.Recorded ? ticket.NetTonnes : 0),
            Unmatched = group.Count(ticket => !activeMatches.Any(match => match.WeighbridgeTicketId == ticket.Id))
        }).SingleOrDefaultAsync(cancellationToken);
        IQueryable<WeighbridgeTicket> ordered = query.OrderByDescending(x => x.TicketDate)
            .ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        if (page.HasValue && pageSize.HasValue)
            ordered = ordered.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        var rows = await ordered.AsSingleQuery().Select(ticket => new
        {
            Ticket = ticket,
            Mill = context.Mills.Single(mill => mill.TenantId == tenantId && mill.FarmId == farmId && mill.Id == ticket.MillId),
            Evidence = context.EvidenceDocuments.Where(evidence => evidence.TenantId == tenantId &&
                evidence.FarmId == farmId && evidence.WeighbridgeTicketId == ticket.Id)
                .OrderByDescending(evidence => evidence.UploadedAt).ThenBy(evidence => evidence.Id).ToArray(),
            Matches = activeMatches.Where(match => match.WeighbridgeTicketId == ticket.Id)
                .OrderBy(match => match.CreatedAt).ThenBy(match => match.Id).ToArray()
        }).ToArrayAsync(cancellationToken);
        return new(rows.Select(x => x.Ticket).ToArray(), rows.Select(x => x.Mill).DistinctBy(x => x.Id).ToArray(),
            rows.SelectMany(x => x.Evidence).ToArray(), rows.SelectMany(x => x.Matches).ToArray(), summary?.Total ?? 0,
            summary?.Unmatched ?? 0, summary?.Tonnes ?? 0);
    }

    public async Task<IMillRecordsTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken) => new MillRecordsTransaction(
        await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken));

    public async Task<IReadOnlyList<Mill>> GetMillsAsync(Guid tenantId, Guid farmId,
        bool includeInactive, CancellationToken cancellationToken)
    {
        IQueryable<Mill> query = context.Mills.AsNoTracking().Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId);
        if (!includeInactive) query = query.Where(x => x.Active);
        return await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<Mill?> GetMillAsync(Guid tenantId, Guid farmId, Guid id, bool trackChanges,
        CancellationToken cancellationToken)
    {
        IQueryable<Mill> query = context.Mills.Where(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.Id == id);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WeighbridgeTicket>> GetTicketsAsync(Guid tenantId,
        Guid farmId, TicketFilter filter, CancellationToken cancellationToken)
    {
        return await TicketQuery(tenantId, farmId, filter).OrderByDescending(x => x.TicketDate)
            .ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    private IQueryable<WeighbridgeTicket> TicketQuery(Guid tenantId, Guid farmId, TicketFilter filter)
    {
        IQueryable<WeighbridgeTicket> query = context.WeighbridgeTickets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId);
        if (filter.From.HasValue) query = query.Where(x => x.TicketDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.TicketDate <= filter.To.Value);
        if (filter.MillId.HasValue) query = query.Where(x => x.MillId == filter.MillId.Value);
        if (filter.FieldId.HasValue) query = query.Where(x => x.FieldId == filter.FieldId.Value);
        if (filter.CropCycleId.HasValue) query = query.Where(x => x.CropCycleId == filter.CropCycleId.Value);
        if (Enum.TryParse(filter.Status, true, out WeighbridgeTicketStatus status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string term = filter.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.TicketReference, $"%{term}%") ||
                (x.SourceReference != null && EF.Functions.ILike(x.SourceReference, $"%{term}%")));
        }
        return query;
    }

    public Task<WeighbridgeTicket?> GetTicketAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<WeighbridgeTicket> query = context.WeighbridgeTickets.Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.Id == id);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<WeighbridgeTicket?> GetTicketByRecordingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.WeighbridgeTickets
        .AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.RecordingIdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<GrowerStatement>> GetStatementsAsync(Guid tenantId,
        Guid farmId, StatementFilter filter, CancellationToken cancellationToken)
    {
        return await StatementQuery(tenantId, farmId, filter).OrderByDescending(x => x.PeriodEnd)
            .ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    private IQueryable<GrowerStatement> StatementQuery(Guid tenantId, Guid farmId,
        StatementFilter filter)
    {
        IQueryable<GrowerStatement> query = context.GrowerStatements.AsNoTracking().Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId);
        if (filter.From.HasValue) query = query.Where(x => x.PeriodEnd >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.PeriodStart <= filter.To.Value);
        if (filter.MillId.HasValue) query = query.Where(x => x.MillId == filter.MillId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string term = filter.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.StatementReference, $"%{term}%"));
        }
        return query;
    }

    public Task<GrowerStatement?> GetStatementAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<GrowerStatement> query = context.GrowerStatements.Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.Id == id);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<GrowerStatement?> GetStatementByRecordingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.GrowerStatements
        .AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.RecordingIdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<StatementTicketMatch>> GetMatchesForStatementAsync(
        Guid tenantId, Guid farmId, Guid statementId, CancellationToken cancellationToken) =>
        await context.StatementTicketMatches.AsNoTracking().Where(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.GrowerStatementId == statementId)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StatementTicketMatch>> GetMatchesForTicketAsync(
        Guid tenantId, Guid farmId, Guid ticketId, CancellationToken cancellationToken) =>
        await context.StatementTicketMatches.AsNoTracking().Where(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.WeighbridgeTicketId == ticketId)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<StatementTicketMatch?> GetMatchAsync(Guid tenantId, Guid farmId, Guid id,
        CancellationToken cancellationToken) => context.StatementTicketMatches.AsNoTracking()
        .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId && x.Id == id,
            cancellationToken);

    public Task<StatementTicketMatch?> GetMatchByIdempotencyKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.StatementTicketMatches
        .AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<EvidenceDocument>> GetTicketEvidenceAsync(Guid tenantId,
        Guid farmId, Guid ticketId, CancellationToken cancellationToken) => await context
        .EvidenceDocuments.AsNoTracking().Where(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.WeighbridgeTicketId == ticketId).OrderByDescending(x => x.UploadedAt)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EvidenceDocument>> GetStatementEvidenceAsync(Guid tenantId,
        Guid farmId, Guid statementId, CancellationToken cancellationToken) => await context
        .EvidenceDocuments.AsNoTracking().Where(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.GrowerStatementId == statementId).OrderByDescending(x => x.UploadedAt)
        .ToListAsync(cancellationToken);

    public Task<EvidenceDocument?> GetEvidenceAsync(Guid tenantId, Guid farmId, Guid evidenceId,
        CancellationToken cancellationToken) => context.EvidenceDocuments.AsNoTracking()
        .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.Id == evidenceId, cancellationToken);

    public void Add(Mill mill) => context.Mills.Add(mill);
    public void Add(WeighbridgeTicket ticket) => context.WeighbridgeTickets.Add(ticket);
    public void Add(GrowerStatement statement) => context.GrowerStatements.Add(statement);
    public void Add(StatementTicketMatch match) => context.StatementTicketMatches.Add(match);
    public void Add(EvidenceDocument evidence) => context.EvidenceDocuments.Add(evidence);
    public void Add(MillRecordExport export) => context.MillRecordExports.Add(export);
    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
    public void Add(MillRecordAuditEventLink auditLink) => context.MillRecordAuditEventLinks.Add(auditLink);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { return await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        { throw new ConflictException("This mill record changed before the action completed. Refresh and try again."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres &&
            (postgres.SqlState == PostgresErrorCodes.UniqueViolation ||
             postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
             postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation ||
             postgres.SqlState == PostgresErrorCodes.CheckViolation ||
             postgres.SqlState == PostgresErrorCodes.RaiseException))
        { throw new ConflictException("The mill-record operation conflicts with an authoritative scope, duplicate, capacity, or idempotency rule."); }
    }

    private sealed class MillRecordsTransaction(IDbContextTransaction transaction) : IMillRecordsTransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try { await transaction.CommitAsync(cancellationToken); }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
            { throw new ConflictException("The mill-record operation raced with another authoritative operation. Refresh and retry."); }
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
