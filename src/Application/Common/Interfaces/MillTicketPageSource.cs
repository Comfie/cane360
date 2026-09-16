using Cane360.Domain.MillRecords;

namespace Cane360.Application.Common.Interfaces;

public sealed record MillTicketPageSource(IReadOnlyList<WeighbridgeTicket> Tickets,
    IReadOnlyList<Mill> Mills, IReadOnlyList<EvidenceDocument> Evidence,
    IReadOnlyList<StatementTicketMatch> Matches, int TotalCount, int UnmatchedCount,
    decimal RecordedNetTonnes);
