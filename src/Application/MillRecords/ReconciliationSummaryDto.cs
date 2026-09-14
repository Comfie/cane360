namespace Cane360.Application.MillRecords;

public sealed record ReconciliationSummaryDto(Guid GrowerStatementId,
    decimal StatementTonnes, decimal MatchedTicketTonnes, decimal TonnesVariance,
    decimal StatementAmountUsd, decimal? MatchedAmountUsd, decimal? AmountVarianceUsd,
    string AmountStatus, string Status, bool HasCrossStatementTicketReuse,
    IReadOnlyList<StatementTicketMatchDto> Matches);
