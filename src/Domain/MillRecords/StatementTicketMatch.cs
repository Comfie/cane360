namespace Cane360.Domain.MillRecords;

public sealed class StatementTicketMatch : BaseEntity
{
    private StatementTicketMatch() { }

    private StatementTicketMatch(Guid tenantId, Guid farmId, Guid growerStatementId,
        Guid weighbridgeTicketId, decimal matchedTonnes, decimal? matchedAmountUsd,
        bool completesMatching, string? reason, string createdByUserId, DateTimeOffset createdAt,
        StatementTicketMatchAction action, Guid? reversesMatchId, string idempotencyKey)
    {
        if (matchedTonnes <= 0) throw new ArgumentOutOfRangeException(nameof(matchedTonnes), "Matched tonnes must be positive.");
        if (decimal.Round(matchedTonnes, 3, MidpointRounding.AwayFromZero) != matchedTonnes)
            throw new ArgumentException("Matched tonnes support at most three decimal places.", nameof(matchedTonnes));
        if (matchedAmountUsd is < 0) throw new ArgumentOutOfRangeException(nameof(matchedAmountUsd), "Matched amount cannot be negative.");
        if (matchedAmountUsd.HasValue && decimal.Round(matchedAmountUsd.Value, 2, MidpointRounding.AwayFromZero) != matchedAmountUsd.Value)
            throw new ArgumentException("USD amounts support at most two decimal places.", nameof(matchedAmountUsd));
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (action == StatementTicketMatchAction.Reversed) ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        TenantId = tenantId;
        FarmId = farmId;
        GrowerStatementId = growerStatementId;
        WeighbridgeTicketId = weighbridgeTicketId;
        MatchedTonnes = matchedTonnes;
        MatchedAmountUsd = matchedAmountUsd;
        CompletesMatching = completesMatching;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
        Action = action;
        ReversesMatchId = reversesMatchId;
        IdempotencyKey = idempotencyKey.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid GrowerStatementId { get; private set; }
    public Guid WeighbridgeTicketId { get; private set; }
    public decimal MatchedTonnes { get; private set; }
    public decimal? MatchedAmountUsd { get; private set; }
    public bool CompletesMatching { get; private set; }
    public string? Reason { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public StatementTicketMatchAction Action { get; private set; }
    public Guid? ReversesMatchId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    public static StatementTicketMatch Add(Guid tenantId, Guid farmId, Guid statementId,
        Guid ticketId, decimal matchedTonnes, decimal? matchedAmountUsd, bool completesMatching,
        string? reason, string userId, DateTimeOffset at, string idempotencyKey)
    {
        if (matchedTonnes < 0) throw new ArgumentOutOfRangeException(nameof(matchedTonnes));
        return new(tenantId, farmId, statementId, ticketId, matchedTonnes,
            matchedAmountUsd, completesMatching, reason, userId, at,
            StatementTicketMatchAction.Added, null, idempotencyKey);
    }

    public static StatementTicketMatch Reverse(StatementTicketMatch original, string reason,
        string userId, DateTimeOffset at, string idempotencyKey)
    {
        if (original.Action != StatementTicketMatchAction.Added)
            throw new InvalidOperationException("Only an added match can be reversed.");
        return new(original.TenantId, original.FarmId, original.GrowerStatementId,
            original.WeighbridgeTicketId, original.MatchedTonnes, original.MatchedAmountUsd,
            original.CompletesMatching, reason, userId, at,
            StatementTicketMatchAction.Reversed, original.Id, idempotencyKey);
    }
}
