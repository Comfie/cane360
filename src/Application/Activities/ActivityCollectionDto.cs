namespace Cane360.Application.Activities;

public sealed record ActivityCollectionDto(
    IReadOnlyList<ActivityListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
