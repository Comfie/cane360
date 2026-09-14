using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;

namespace Cane360.Application.MillRecords;

public sealed class MillRecordsService(IFarmSetupRepository farms, IMillRecordsRepository records,
    IEvidenceDocumentStorage storage, IUser user, TimeProvider clock) : IMillRecordsService
{
    private const long MaximumEvidenceBytes = 20 * 1024 * 1024;

    public async Task<MillRecordsSessionDto> GetSessionAsync(CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        return new(Role(context), context.Farm.Fields.OrderBy(x => x.Code).Select(field =>
            new MillFieldDto(field.Id, field.Code, field.Name, field.CropCycles
                .OrderByDescending(x => x.StartDate).Select(cycle => new MillCropCycleDto(cycle.Id,
                    cycle.Variety, cycle.Status.ToString(), cycle.HarvestResult?.ActualTonnes))
                .ToArray())).ToArray());
    }

    public async Task<IReadOnlyList<MillDto>> GetMillsAsync(bool includeInactive,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        return (await records.GetMillsAsync(context.Tenant.Id, context.Farm.Id, includeInactive,
            cancellationToken)).Select(Map).ToArray();
    }

    public async Task<MillDto> CreateMillAsync(MillInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        Mill mill = Apply(() => Mill.Create(context.Tenant.Id, context.Farm.Id, input.Code,
            input.Name, input.Location, context.UserId, clock.GetUtcNow()), nameof(input.Code));
        records.Add(mill);
        Audit(context, mill.Id, "MillCreated", null, "Mill reference created.",
            audit => MillRecordAuditEventLink.ForMill(audit.Id, context.Tenant.Id, context.Farm.Id, mill.Id));
        await records.SaveChangesAsync(cancellationToken);
        return Map(mill);
    }

    public async Task<MillDto> UpdateMillAsync(Guid millId, MillInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        Mill mill = await RequireMillAsync(context, millId, true, cancellationToken);
        Apply(() => mill.Update(input.Code, input.Name, input.Location, input.ExpectedVersion),
            nameof(input.ExpectedVersion));
        Audit(context, mill.Id, "MillChanged", null, "Mill reference details changed.",
            audit => MillRecordAuditEventLink.ForMill(audit.Id, context.Tenant.Id, context.Farm.Id, mill.Id));
        await records.SaveChangesAsync(cancellationToken);
        return Map(mill);
    }

    public async Task<MillDto> DeactivateMillAsync(Guid millId, VersionedInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        Mill mill = await RequireMillAsync(context, millId, true, cancellationToken);
        Apply(() => mill.Deactivate(input.ExpectedVersion), nameof(input.ExpectedVersion));
        Audit(context, mill.Id, "MillDeactivated", null,
            "Mill reference deactivated; historical records remain available.",
            audit => MillRecordAuditEventLink.ForMill(audit.Id, context.Tenant.Id, context.Farm.Id, mill.Id));
        await records.SaveChangesAsync(cancellationToken);
        return Map(mill);
    }

    public async Task<IReadOnlyList<WeighbridgeTicketDto>> GetTicketsAsync(TicketFilter filter,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        IReadOnlyList<WeighbridgeTicket> tickets = await records.GetTicketsAsync(context.Tenant.Id,
            context.Farm.Id, filter, cancellationToken);
        var results = new List<WeighbridgeTicketDto>();
        foreach (WeighbridgeTicket ticket in tickets)
            results.Add(await MapTicketAsync(context, ticket, tickets, cancellationToken));
        if (string.IsNullOrWhiteSpace(filter.MatchStatus)) return results;
        bool matched = filter.MatchStatus.Equals("Matched", StringComparison.OrdinalIgnoreCase);
        return results.Where(x => matched ? x.MatchedStatementIds.Count != 0 :
            x.MatchedStatementIds.Count == 0).ToArray();
    }

    public async Task<WeighbridgeTicketDto> GetTicketAsync(Guid ticketId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        WeighbridgeTicket ticket = await RequireTicketAsync(context, ticketId, false, cancellationToken);
        IReadOnlyList<WeighbridgeTicket> tickets = await records.GetTicketsAsync(context.Tenant.Id,
            context.Farm.Id, new(null, null, null, null, null, null, null, null), cancellationToken);
        return await MapTicketAsync(context, ticket, tickets, cancellationToken);
    }

    public async Task<WeighbridgeTicketDto> CreateTicketAsync(TicketInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        await RequireMillAsync(context, input.MillId, false, cancellationToken);
        ValidateAssociation(context, input.FieldId, input.CropCycleId);
        WeighbridgeTicket ticket = Apply(() => WeighbridgeTicket.CreateDraft(context.Tenant.Id,
            context.Farm.Id, input.MillId, input.TicketReference, input.TicketDate,
            input.GrossTonnes, input.TareTonnes, input.NetTonnes, input.FieldId,
            input.CropCycleId, input.SourceReference, input.Notes, context.UserId,
            clock.GetUtcNow()), nameof(input.NetTonnes));
        records.Add(ticket);
        Audit(context, ticket.Id, "TicketCreated", null, "Weighbridge ticket draft created.",
            audit => MillRecordAuditEventLink.ForTicket(audit.Id, context.Tenant.Id, context.Farm.Id, ticket.Id));
        await records.SaveChangesAsync(cancellationToken);
        return await MapTicketAsync(context, ticket, [ticket], cancellationToken);
    }

    public async Task<WeighbridgeTicketDto> UpdateTicketAsync(Guid ticketId, TicketInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        await RequireMillAsync(context, input.MillId, false, cancellationToken);
        ValidateAssociation(context, input.FieldId, input.CropCycleId);
        WeighbridgeTicket ticket = await RequireTicketAsync(context, ticketId, true, cancellationToken);
        Apply(() => ticket.UpdateDraft(input.MillId, input.TicketReference, input.TicketDate,
            input.GrossTonnes, input.TareTonnes, input.NetTonnes, input.FieldId,
            input.CropCycleId, input.SourceReference, input.Notes, input.ExpectedVersion),
            nameof(input.ExpectedVersion));
        Audit(context, ticket.Id, "TicketDraftChanged", null, "Weighbridge ticket draft changed.",
            audit => MillRecordAuditEventLink.ForTicket(audit.Id, context.Tenant.Id, context.Farm.Id, ticket.Id));
        await records.SaveChangesAsync(cancellationToken);
        return await MapTicketAsync(context, ticket, [ticket], cancellationToken);
    }

    public async Task<WeighbridgeTicketDto> RecordTicketAsync(Guid ticketId, RecordInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        await using IMillRecordsTransaction transaction = await records.BeginSerializableTransactionAsync(cancellationToken);
        WeighbridgeTicket? existing = await records.GetTicketByRecordingKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.Id != ticketId) throw new ConflictException("This recording key belongs to another ticket.");
            await transaction.CommitAsync(cancellationToken);
            return await MapTicketAsync(context, existing, [existing], cancellationToken);
        }
        WeighbridgeTicket ticket = await RequireTicketAsync(context, ticketId, true, cancellationToken);
        Apply(() => ticket.Record(context.UserId, clock.GetUtcNow(), input.IdempotencyKey,
            input.ExpectedVersion), nameof(input.ExpectedVersion));
        Audit(context, ticket.Id, "TicketRecorded", null,
            "Weighbridge ticket recorded as authoritative mill evidence.",
            audit => MillRecordAuditEventLink.ForTicket(audit.Id, context.Tenant.Id, context.Farm.Id, ticket.Id));
        await records.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapTicketAsync(context, ticket, [ticket], cancellationToken);
    }

    public async Task<WeighbridgeTicketDto> CorrectTicketAsync(Guid ticketId,
        CorrectTicketInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireGrower(context);
        await using IMillRecordsTransaction transaction = await records.BeginSerializableTransactionAsync(cancellationToken);
        WeighbridgeTicket? existing = await records.GetTicketByRecordingKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.CorrectsTicketId != ticketId) throw new ConflictException("This correction key belongs to another ticket.");
            await transaction.CommitAsync(cancellationToken);
            return await MapTicketAsync(context, existing, [existing], cancellationToken);
        }
        WeighbridgeTicket original = await RequireTicketAsync(context, ticketId, false, cancellationToken);
        if (input.Replacement.MillId != original.MillId)
            throw Validation(nameof(input.Replacement.MillId), "A ticket correction must retain the authoritative mill.");
        ValidateAssociation(context, input.Replacement.FieldId, input.Replacement.CropCycleId);
        WeighbridgeTicket replacement = Apply(() => WeighbridgeTicket.CreateCorrection(original,
            input.Replacement.TicketReference, input.Replacement.TicketDate,
            input.Replacement.GrossTonnes, input.Replacement.TareTonnes,
            input.Replacement.NetTonnes, input.Replacement.FieldId,
            input.Replacement.CropCycleId, input.Replacement.SourceReference,
            input.Replacement.Notes, input.Reason, context.UserId, clock.GetUtcNow()),
            nameof(input.Reason));
        replacement.Record(context.UserId, clock.GetUtcNow(), input.IdempotencyKey, replacement.Version);
        records.Add(replacement);
        Audit(context, replacement.Id, "TicketCorrected", input.Reason,
            "Recorded replacement ticket appended; original ticket preserved.",
            audit => MillRecordAuditEventLink.ForTicket(audit.Id, context.Tenant.Id, context.Farm.Id, replacement.Id));
        await records.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapTicketAsync(context, replacement, [original, replacement], cancellationToken);
    }

    public async Task<IReadOnlyList<GrowerStatementDto>> GetStatementsAsync(StatementFilter filter,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        IReadOnlyList<GrowerStatement> statements = await records.GetStatementsAsync(context.Tenant.Id,
            context.Farm.Id, filter, cancellationToken);
        var results = new List<GrowerStatementDto>();
        foreach (GrowerStatement statement in statements)
            results.Add(await MapStatementAsync(context, statement, statements, cancellationToken));
        return string.IsNullOrWhiteSpace(filter.MatchStatus) ? results : results.Where(x =>
            x.Reconciliation.Status.Equals(filter.MatchStatus, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public async Task<GrowerStatementDto> GetStatementAsync(Guid statementId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        GrowerStatement statement = await RequireStatementAsync(context, statementId, false, cancellationToken);
        IReadOnlyList<GrowerStatement> statements = await records.GetStatementsAsync(context.Tenant.Id,
            context.Farm.Id, new(null, null, null, null, null), cancellationToken);
        return await MapStatementAsync(context, statement, statements, cancellationToken);
    }

    public async Task<GrowerStatementDto> CreateStatementAsync(StatementInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        await RequireMillAsync(context, input.MillId, false, cancellationToken);
        GrowerStatement statement = Apply(() => GrowerStatement.CreateDraft(context.Tenant.Id,
            context.Farm.Id, input.MillId, input.StatementReference, input.PeriodStart,
            input.PeriodEnd, input.TotalTonnes, input.TotalAmountUsd, input.Notes,
            context.UserId, clock.GetUtcNow()), nameof(input.PeriodEnd));
        records.Add(statement);
        Audit(context, statement.Id, "StatementCreated", null, "Grower statement draft created.",
            audit => MillRecordAuditEventLink.ForStatement(audit.Id, context.Tenant.Id, context.Farm.Id, statement.Id));
        await records.SaveChangesAsync(cancellationToken);
        return await MapStatementAsync(context, statement, [statement], cancellationToken);
    }

    public async Task<GrowerStatementDto> UpdateStatementAsync(Guid statementId,
        StatementInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        await RequireMillAsync(context, input.MillId, false, cancellationToken);
        GrowerStatement statement = await RequireStatementAsync(context, statementId, true, cancellationToken);
        Apply(() => statement.UpdateDraft(input.MillId, input.StatementReference,
            input.PeriodStart, input.PeriodEnd, input.TotalTonnes, input.TotalAmountUsd,
            input.Notes, input.ExpectedVersion), nameof(input.ExpectedVersion));
        Audit(context, statement.Id, "StatementDraftChanged", null, "Grower statement draft changed.",
            audit => MillRecordAuditEventLink.ForStatement(audit.Id, context.Tenant.Id, context.Farm.Id, statement.Id));
        await records.SaveChangesAsync(cancellationToken);
        return await MapStatementAsync(context, statement, [statement], cancellationToken);
    }

    public async Task<GrowerStatementDto> RecordStatementAsync(Guid statementId,
        RecordInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        await using IMillRecordsTransaction transaction = await records.BeginSerializableTransactionAsync(cancellationToken);
        GrowerStatement? existing = await records.GetStatementByRecordingKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.Id != statementId) throw new ConflictException("This recording key belongs to another statement.");
            await transaction.CommitAsync(cancellationToken);
            return await MapStatementAsync(context, existing, [existing], cancellationToken);
        }
        GrowerStatement statement = await RequireStatementAsync(context, statementId, true, cancellationToken);
        bool hasEvidence = (await records.GetStatementEvidenceAsync(context.Tenant.Id,
            context.Farm.Id, statement.Id, cancellationToken)).Count != 0;
        Apply(() => statement.Record(context.UserId, clock.GetUtcNow(), input.IdempotencyKey,
            input.ExpectedVersion, hasEvidence), nameof(input.ExpectedVersion));
        Audit(context, statement.Id, "StatementRecorded", null,
            "Grower statement totals recorded as authoritative evidence.",
            audit => MillRecordAuditEventLink.ForStatement(audit.Id, context.Tenant.Id, context.Farm.Id, statement.Id));
        await records.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapStatementAsync(context, statement, [statement], cancellationToken);
    }

    public async Task<GrowerStatementDto> CorrectStatementAsync(Guid statementId,
        CorrectStatementInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireGrower(context);
        GrowerStatement original = await RequireStatementAsync(context, statementId, false, cancellationToken);
        if (input.Replacement.MillId != original.MillId)
            throw Validation(nameof(input.Replacement.MillId), "A statement correction must retain the authoritative mill.");
        GrowerStatement replacement = Apply(() => GrowerStatement.CreateCorrection(original,
            input.Replacement.StatementReference, input.Replacement.PeriodStart,
            input.Replacement.PeriodEnd, input.Replacement.TotalTonnes,
            input.Replacement.TotalAmountUsd, input.Replacement.Notes, input.Reason,
            context.UserId, clock.GetUtcNow()), nameof(input.Reason));
        records.Add(replacement);
        Audit(context, replacement.Id, "StatementCorrectionCreated", input.Reason,
            "Replacement statement draft appended; original statement preserved.",
            audit => MillRecordAuditEventLink.ForStatement(audit.Id, context.Tenant.Id, context.Farm.Id, replacement.Id));
        await records.SaveChangesAsync(cancellationToken);
        return await MapStatementAsync(context, replacement, [original, replacement], cancellationToken);
    }

    public async Task<ReconciliationSummaryDto> AddMatchAsync(Guid statementId,
        AddMatchInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        await using IMillRecordsTransaction transaction = await records.BeginSerializableTransactionAsync(cancellationToken);
        StatementTicketMatch? retry = await records.GetMatchByIdempotencyKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (retry is not null)
        {
            if (retry.GrowerStatementId != statementId || retry.WeighbridgeTicketId != input.WeighbridgeTicketId)
                throw new ConflictException("This matching key belongs to another reconciliation action.");
            await transaction.CommitAsync(cancellationToken);
            return await BuildReconciliationAsync(context, statementId, cancellationToken);
        }
        GrowerStatement statement = await RequireStatementAsync(context, statementId, false, cancellationToken);
        WeighbridgeTicket ticket = await RequireTicketAsync(context, input.WeighbridgeTicketId, false, cancellationToken);
        if (statement.Status != GrowerStatementStatus.Recorded || ticket.Status != WeighbridgeTicketStatus.Recorded)
            throw new ConflictException("Only recorded statements and tickets can be matched.");
        if (statement.Version != input.ExpectedStatementVersion || ticket.Version != input.ExpectedTicketVersion)
            throw new ConflictException("The statement or ticket changed after it was loaded. Refresh and try again.");
        if (statement.MillId != ticket.MillId)
            throw Validation(nameof(input.WeighbridgeTicketId), "The statement and ticket must belong to the same mill.");
        IReadOnlyList<StatementTicketMatch> ticketEvents = await records.GetMatchesForTicketAsync(
            context.Tenant.Id, context.Farm.Id, ticket.Id, cancellationToken);
        IReadOnlyList<StatementTicketMatch> active = ActiveMatches(ticketEvents);
        if (active.Any(x => x.GrowerStatementId == statement.Id))
            throw new ConflictException("This ticket is already actively matched to the statement.");
        decimal available = ticket.NetTonnes - active.Sum(x => x.MatchedTonnes);
        decimal matchedTonnes = input.MatchedTonnes ?? available;
        if (matchedTonnes <= 0 || matchedTonnes > available)
            throw Validation(nameof(input.MatchedTonnes), "Matched tonnes must be positive and cannot exceed the ticket's available net tonnes.");
        if (matchedTonnes != available && string.IsNullOrWhiteSpace(input.Reason))
            throw Validation(nameof(input.Reason), "A reason is required for a partial ticket match.");
        StatementTicketMatch match = Apply(() => StatementTicketMatch.Add(context.Tenant.Id,
            context.Farm.Id, statement.Id, ticket.Id, matchedTonnes, input.MatchedAmountUsd,
            input.CompletesMatching, input.Reason, context.UserId, clock.GetUtcNow(),
            input.IdempotencyKey), nameof(input.MatchedTonnes));
        records.Add(match);
        Audit(context, match.Id, "TicketMatched", input.Reason,
            "Recorded ticket matched to a grower statement for reconciliation.",
            audit => MillRecordAuditEventLink.ForMatch(audit.Id, context.Tenant.Id, context.Farm.Id, match.Id));
        await records.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildReconciliationAsync(context, statementId, cancellationToken);
    }

    public async Task<ReconciliationSummaryDto> ReverseMatchAsync(Guid statementId, Guid matchId,
        ReverseMatchInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireGrower(context);
        await using IMillRecordsTransaction transaction = await records.BeginSerializableTransactionAsync(cancellationToken);
        StatementTicketMatch? retry = await records.GetMatchByIdempotencyKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (retry is not null)
        {
            if (retry.ReversesMatchId != matchId) throw new ConflictException("This reversal key belongs to another match.");
            await transaction.CommitAsync(cancellationToken);
            return await BuildReconciliationAsync(context, statementId, cancellationToken);
        }
        StatementTicketMatch original = await records.GetMatchAsync(context.Tenant.Id,
            context.Farm.Id, matchId, cancellationToken) ?? throw new NotFoundException(matchId.ToString(), "Statement ticket match");
        if (original.GrowerStatementId != statementId) throw new NotFoundException(matchId.ToString(), "Statement ticket match");
        IReadOnlyList<StatementTicketMatch> events = await records.GetMatchesForStatementAsync(
            context.Tenant.Id, context.Farm.Id, statementId, cancellationToken);
        if (!ActiveMatches(events).Any(x => x.Id == matchId))
            throw new ConflictException("This match is no longer active.");
        StatementTicketMatch reversal = Apply(() => StatementTicketMatch.Reverse(original,
            input.Reason, context.UserId, clock.GetUtcNow(), input.IdempotencyKey), nameof(input.Reason));
        records.Add(reversal);
        Audit(context, reversal.Id, "TicketMatchReversed", input.Reason,
            "Authoritative ticket match reversal appended; original match preserved.",
            audit => MillRecordAuditEventLink.ForMatch(audit.Id, context.Tenant.Id, context.Farm.Id, reversal.Id));
        await records.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildReconciliationAsync(context, statementId, cancellationToken);
    }

    public async Task<ReconciliationSummaryDto> GetReconciliationAsync(Guid statementId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        return await BuildReconciliationAsync(context, statementId, cancellationToken);
    }

    public async Task<IReadOnlyList<CandidateTicketDto>> GetCandidateTicketsAsync(Guid statementId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        GrowerStatement statement = await RequireStatementAsync(context, statementId, false, cancellationToken);
        IReadOnlyList<WeighbridgeTicket> tickets = await records.GetTicketsAsync(context.Tenant.Id,
            context.Farm.Id, new(statement.PeriodStart, statement.PeriodEnd, statement.MillId,
                null, null, WeighbridgeTicketStatus.Recorded.ToString(), null, null), cancellationToken);
        var result = new List<CandidateTicketDto>();
        foreach (WeighbridgeTicket ticket in CurrentTickets(tickets))
        {
            IReadOnlyList<StatementTicketMatch> active = ActiveMatches(await records.GetMatchesForTicketAsync(
                context.Tenant.Id, context.Farm.Id, ticket.Id, cancellationToken));
            decimal available = ticket.NetTonnes - active.Sum(x => x.MatchedTonnes);
            if (available <= 0 || active.Any(x => x.GrowerStatementId == statementId)) continue;
            (string? fieldName, string? cycleLabel, _) = Association(context, ticket);
            result.Add(new(ticket.Id, ticket.TicketReference, ticket.TicketDate.ToString("yyyy-MM-dd"),
                ticket.NetTonnes, available, ticket.Version, ticket.FieldId, fieldName, ticket.CropCycleId,
                cycleLabel, active.Count != 0));
        }
        return result;
    }

    public Task<EvidenceDocumentDto> UploadTicketEvidenceAsync(Guid ticketId, EvidenceUpload input,
        CancellationToken cancellationToken) => UploadEvidenceAsync(ticketId, null, input, cancellationToken);

    public Task<EvidenceDocumentDto> UploadStatementEvidenceAsync(Guid statementId,
        EvidenceUpload input, CancellationToken cancellationToken) => UploadEvidenceAsync(null,
            statementId, input, cancellationToken);

    public async Task<EvidenceDownload> OpenEvidenceAsync(Guid evidenceId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        EvidenceDocument evidence = await records.GetEvidenceAsync(context.Tenant.Id,
            context.Farm.Id, evidenceId, cancellationToken) ?? throw new NotFoundException(
                evidenceId.ToString(), "Evidence document");
        Audit(context, evidence.Id, "EvidenceAccessed", null,
            "Private mill-record evidence accessed through an authenticated endpoint.",
            audit => MillRecordAuditEventLink.ForEvidence(audit.Id, context.Tenant.Id, context.Farm.Id, evidence.Id));
        await records.SaveChangesAsync(cancellationToken);
        Stream content = await storage.OpenReadAsync(evidence.StorageKey, cancellationToken);
        return new(content, evidence.OriginalFileName, evidence.ContentType);
    }

    public async Task RecordExportAsync(string kind, string filters,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        MillRecordExport export = MillRecordExport.Create(context.Tenant.Id, context.Farm.Id,
            kind, filters, context.UserId, clock.GetUtcNow());
        records.Add(export);
        Audit(context, export.Id, "ReconciliationExported", null,
            "Filtered mill-record reconciliation evidence exported.",
            audit => MillRecordAuditEventLink.ForExport(audit.Id, context.Tenant.Id,
                context.Farm.Id, export.Id));
        await records.SaveChangesAsync(cancellationToken);
    }

    private async Task<EvidenceDocumentDto> UploadEvidenceAsync(Guid? ticketId, Guid? statementId,
        EvidenceUpload input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        if (input.Length <= 0 || input.Length > MaximumEvidenceBytes)
            throw Validation(nameof(input.Content), "Evidence must contain between 1 byte and 20 MB.");
        if (ticketId.HasValue) await RequireTicketAsync(context, ticketId.Value, false, cancellationToken);
        if (statementId.HasValue) await RequireStatementAsync(context, statementId.Value, false, cancellationToken);
        StoredEvidence stored = await storage.SaveAsync(input.Content, input.FileName, cancellationToken);
        if (stored.SizeBytes != input.Length)
            throw Validation(nameof(input.Content), "The uploaded evidence length did not match the request.");
        EvidenceDocument evidence = ticketId.HasValue
            ? EvidenceDocument.ForTicket(context.Tenant.Id, context.Farm.Id, ticketId.Value,
                input.FileName, input.ContentType, stored.SizeBytes, stored.StorageKey,
                context.UserId, clock.GetUtcNow())
            : EvidenceDocument.ForStatement(context.Tenant.Id, context.Farm.Id, statementId!.Value,
                input.FileName, input.ContentType, stored.SizeBytes, stored.StorageKey,
                context.UserId, clock.GetUtcNow());
        records.Add(evidence);
        Audit(context, evidence.Id, statementId.HasValue ? "StatementUploaded" : "TicketEvidenceAttached",
            null, "Private source evidence stored and linked to its authoritative record.",
            audit => MillRecordAuditEventLink.ForEvidence(audit.Id, context.Tenant.Id, context.Farm.Id, evidence.Id));
        await records.SaveChangesAsync(cancellationToken);
        return Map(evidence);
    }

    private async Task<WeighbridgeTicketDto> MapTicketAsync(Context context,
        WeighbridgeTicket ticket, IReadOnlyList<WeighbridgeTicket> allTickets,
        CancellationToken cancellationToken)
    {
        Mill mill = await RequireMillAsync(context, ticket.MillId, false, cancellationToken);
        IReadOnlyList<EvidenceDocument> evidence = await records.GetTicketEvidenceAsync(
            context.Tenant.Id, context.Farm.Id, ticket.Id, cancellationToken);
        IReadOnlyList<StatementTicketMatch> active = ActiveMatches(await records.GetMatchesForTicketAsync(
            context.Tenant.Id, context.Farm.Id, ticket.Id, cancellationToken));
        Guid? correctedBy = allTickets.SingleOrDefault(x => x.CorrectsTicketId == ticket.Id &&
            x.Status == WeighbridgeTicketStatus.Recorded)?.Id;
        (string? fieldName, string? cycleLabel, decimal? harvest) = Association(context, ticket);
        Guid[] statementIds = active.Select(x => x.GrowerStatementId).Distinct().ToArray();
        return new(ticket.Id, mill.Id, mill.Code, mill.Name, ticket.TicketReference,
            ticket.TicketDate.ToString("yyyy-MM-dd"), ticket.GrossTonnes, ticket.TareTonnes,
            ticket.NetTonnes, ticket.FieldId, fieldName, ticket.CropCycleId, cycleLabel, harvest,
            ticket.SourceReference, ticket.Notes, ticket.Status.ToString(), ticket.Version,
            ticket.CreatedAt, ticket.RecordedAt, ticket.CorrectsTicketId, ticket.CorrectionReason,
            correctedBy, correctedBy is null, evidence.Select(Map).ToArray(), statementIds,
            statementIds.Length > 1);
    }

    private async Task<GrowerStatementDto> MapStatementAsync(Context context,
        GrowerStatement statement, IReadOnlyList<GrowerStatement> allStatements,
        CancellationToken cancellationToken)
    {
        Mill mill = await RequireMillAsync(context, statement.MillId, false, cancellationToken);
        IReadOnlyList<EvidenceDocument> evidence = await records.GetStatementEvidenceAsync(
            context.Tenant.Id, context.Farm.Id, statement.Id, cancellationToken);
        Guid? correctedBy = allStatements.SingleOrDefault(x => x.CorrectsStatementId == statement.Id &&
            x.Status == GrowerStatementStatus.Recorded)?.Id;
        ReconciliationSummaryDto reconciliation = await BuildReconciliationAsync(context,
            statement.Id, cancellationToken, statement);
        return new(statement.Id, mill.Id, mill.Code, mill.Name, statement.StatementReference,
            statement.PeriodStart.ToString("yyyy-MM-dd"), statement.PeriodEnd.ToString("yyyy-MM-dd"),
            statement.TotalTonnes, statement.TotalAmountUsd, statement.Notes,
            statement.Status.ToString(), statement.Version, statement.CreatedAt,
            statement.RecordedAt, statement.CorrectsStatementId, statement.CorrectionReason,
            correctedBy, correctedBy is null, evidence.Select(Map).ToArray(), reconciliation);
    }

    private async Task<ReconciliationSummaryDto> BuildReconciliationAsync(Context context,
        Guid statementId, CancellationToken cancellationToken, GrowerStatement? supplied = null)
    {
        GrowerStatement statement = supplied ?? await RequireStatementAsync(context, statementId,
            false, cancellationToken);
        IReadOnlyList<StatementTicketMatch> events = await records.GetMatchesForStatementAsync(
            context.Tenant.Id, context.Farm.Id, statementId, cancellationToken);
        IReadOnlyList<StatementTicketMatch> active = ActiveMatches(events);
        decimal matchedTonnes = active.Sum(x => x.MatchedTonnes);
        decimal tonnesVariance = statement.TotalTonnes - matchedTonnes;
        ReconciliationStatus status = MillReconciliationMath.Status(statement.TotalTonnes,
            matchedTonnes, active.Count, active.Any(x => x.CompletesMatching));
        int amounts = active.Count(x => x.MatchedAmountUsd.HasValue);
        decimal? matchedAmount = amounts == active.Count && active.Count != 0
            ? active.Sum(x => x.MatchedAmountUsd!.Value) : null;
        AmountReconciliationStatus amountStatus = MillReconciliationMath.AmountStatus(
            statement.TotalAmountUsd, matchedAmount, active.Count, amounts);
        decimal? amountVariance = matchedAmount.HasValue ? statement.TotalAmountUsd - matchedAmount.Value : null;
        var matchDtos = new List<StatementTicketMatchDto>();
        foreach (StatementTicketMatch match in events.Where(x => x.Action == StatementTicketMatchAction.Added))
        {
            WeighbridgeTicket ticket = await RequireTicketAsync(context, match.WeighbridgeTicketId,
                false, cancellationToken);
            StatementTicketMatch? reversal = events.SingleOrDefault(x => x.ReversesMatchId == match.Id);
            matchDtos.Add(new(match.Id, ticket.Id, ticket.TicketReference,
                ticket.TicketDate.ToString("yyyy-MM-dd"), ticket.NetTonnes, match.MatchedTonnes,
                match.MatchedAmountUsd, match.CompletesMatching, match.Reason, match.CreatedAt,
                reversal is null, reversal?.Id));
        }
        bool reused = false;
        foreach (StatementTicketMatch match in active)
        {
            IReadOnlyList<StatementTicketMatch> ticketActive = ActiveMatches(
                await records.GetMatchesForTicketAsync(context.Tenant.Id, context.Farm.Id,
                    match.WeighbridgeTicketId, cancellationToken));
            reused |= ticketActive.Any(x => x.GrowerStatementId != statementId);
        }
        return new(statement.Id, statement.TotalTonnes, matchedTonnes, tonnesVariance,
            statement.TotalAmountUsd, matchedAmount, amountVariance, amountStatus.ToString(),
            status.ToString(), reused, matchDtos);
    }

    private static IReadOnlyList<StatementTicketMatch> ActiveMatches(
        IReadOnlyList<StatementTicketMatch> events)
    {
        HashSet<Guid> reversed = events.Where(x => x.Action == StatementTicketMatchAction.Reversed &&
            x.ReversesMatchId.HasValue).Select(x => x.ReversesMatchId!.Value).ToHashSet();
        return events.Where(x => x.Action == StatementTicketMatchAction.Added && !reversed.Contains(x.Id)).ToArray();
    }

    private static IReadOnlyList<WeighbridgeTicket> CurrentTickets(IReadOnlyList<WeighbridgeTicket> tickets)
    {
        HashSet<Guid> corrected = tickets.Where(x => x.Status == WeighbridgeTicketStatus.Recorded &&
            x.CorrectsTicketId.HasValue).Select(x => x.CorrectsTicketId!.Value).ToHashSet();
        return tickets.Where(x => !corrected.Contains(x.Id)).ToArray();
    }

    private static (string? FieldName, string? CycleLabel, decimal? HarvestTonnes) Association(
        Context context, WeighbridgeTicket ticket)
    {
        Field? field = ticket.FieldId.HasValue ? context.Farm.Fields.SingleOrDefault(x =>
            x.Id == ticket.FieldId.Value) : null;
        CropCycle? cycle = ticket.CropCycleId.HasValue ? field?.CropCycles.SingleOrDefault(x =>
            x.Id == ticket.CropCycleId.Value) : null;
        return (field?.Name, cycle is null ? null : $"{cycle.Variety} · {cycle.StartDate:yyyy}",
            cycle?.HarvestResult?.ActualTonnes);
    }

    private static void ValidateAssociation(Context context, Guid? fieldId, Guid? cropCycleId)
    {
        if (!fieldId.HasValue && !cropCycleId.HasValue) return;
        if (!fieldId.HasValue) throw Validation(nameof(fieldId), "A field is required when a crop cycle is selected.");
        Field field = context.Farm.Fields.SingleOrDefault(x => x.Id == fieldId.Value) ??
            throw new NotFoundException(fieldId.Value.ToString(), "Field");
        if (!cropCycleId.HasValue) return;
        if (!field.CropCycles.Any(x => x.Id == cropCycleId.Value))
            throw new NotFoundException(cropCycleId.Value.ToString(), "Crop cycle");
    }

    private async Task<Context> ContextAsync(bool track, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new ForbiddenAccessException();
        Tenant tenant = await farms.GetTenantForUserAsync(userId, track, cancellationToken) ??
            throw new NotFoundException(userId, "Active grower or farm-manager membership");
        return new(tenant, tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(),
            "Active farm"), userId);
    }

    private Task<Mill> RequireMillAsync(Context context, Guid id, bool track,
        CancellationToken cancellationToken) => RequireAsync(records.GetMillAsync(context.Tenant.Id,
            context.Farm.Id, id, track, cancellationToken), id, "Mill");
    private Task<WeighbridgeTicket> RequireTicketAsync(Context context, Guid id, bool track,
        CancellationToken cancellationToken) => RequireAsync(records.GetTicketAsync(context.Tenant.Id,
            context.Farm.Id, id, track, cancellationToken), id, "Weighbridge ticket");
    private Task<GrowerStatement> RequireStatementAsync(Context context, Guid id, bool track,
        CancellationToken cancellationToken) => RequireAsync(records.GetStatementAsync(context.Tenant.Id,
            context.Farm.Id, id, track, cancellationToken), id, "Grower statement");
    private static async Task<T> RequireAsync<T>(Task<T?> task, Guid id, string subject) where T : class =>
        await task ?? throw new NotFoundException(id.ToString(), subject);

    private void Audit(Context context, Guid subjectId, string action, string? reason,
        string summary, Func<AuditEvent, MillRecordAuditEventLink> link)
    {
        TenantMembership membership = context.Tenant.Memberships.Single(x => x.UserId == context.UserId);
        AuditEvent audit = AuditEvent.Create(context.Tenant.Id, context.Farm.Id, "MillRecords",
            subjectId, action, context.UserId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), reason, summary);
        records.Add(audit);
        records.Add(link(audit));
    }

    private static MillDto Map(Mill mill) => new(mill.Id, mill.Code, mill.Name, mill.Location,
        mill.Active, mill.CreatedAt, mill.Version);
    private static EvidenceDocumentDto Map(EvidenceDocument evidence) => new(evidence.Id,
        evidence.OriginalFileName, evidence.ContentType, evidence.SizeBytes, evidence.UploadedAt);
    private static T Apply<T>(Func<T> action, string field)
    { try { return action(); } catch (InvalidOperationException exception) { throw Validation(field, exception.Message); } catch (ArgumentException exception) { throw Validation(field, exception.Message); } }
    private static void Apply(Action action, string field)
    { try { action(); } catch (InvalidOperationException exception) when (exception.Message.Contains("changed after it was loaded", StringComparison.Ordinal)) { throw new ConflictException(exception.Message); } catch (InvalidOperationException exception) { throw Validation(field, exception.Message); } catch (ArgumentException exception) { throw Validation(field, exception.Message); } }
    private static Application.Common.Exceptions.ValidationException Validation(string field, string message) =>
        new([new FluentValidation.Results.ValidationFailure(field, message)]);
    private static void RequireOperator(Context context)
    { if (Role(context) is not (TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager)) throw new ForbiddenAccessException(); }
    private static void RequireGrower(Context context)
    { if (Role(context) != TenantSecurityRoles.Grower) throw new ForbiddenAccessException(); }
    private static string Role(Context context) => context.Tenant.Memberships.Single(x =>
        x.UserId == context.UserId).SecurityRole;
    private sealed record Context(Tenant Tenant, Farm Farm, string UserId);
}
