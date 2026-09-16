namespace Cane360.Application.MillRecords;

public sealed record GrowerStatementPageDto(IReadOnlyList<GrowerStatementDto> Items,
    int Page, int PageSize, int TotalCount);
