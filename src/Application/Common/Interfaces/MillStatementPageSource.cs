using Cane360.Domain.MillRecords;

namespace Cane360.Application.Common.Interfaces;

public sealed record MillStatementPageSource(IReadOnlyList<GrowerStatement> Statements,
    IReadOnlyList<Mill> Mills, IReadOnlyList<EvidenceDocument> Evidence,
    IReadOnlyList<StatementTicketMatch> Matches, IReadOnlyList<WeighbridgeTicket> Tickets,
    IReadOnlyList<StatementTicketMatch> RelatedTicketMatches, int TotalCount);
