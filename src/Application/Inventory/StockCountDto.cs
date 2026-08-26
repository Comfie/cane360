namespace Cane360.Application.Inventory;

public sealed record StockCountDto(Guid Id, Guid StoreId, string Status, DateOnly EventDate, string Notes,
    string CountingPersons, long? CutoffPostingSequence, DateTimeOffset? StartedAt, DateTimeOffset? ReviewedAt,
    DateTimeOffset? ClosedAt, string? CancellationReason, long Version, IReadOnlyList<StockCountLineDto> Lines);
