namespace Cane360.Application.MillRecords;

public sealed record WeighbridgeTicketPageDto(IReadOnlyList<WeighbridgeTicketDto> Items,
    int Page, int PageSize, int TotalCount, int UnmatchedCount, decimal RecordedNetTonnes);
