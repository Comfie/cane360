using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record ActivityCollectionDto(
    IReadOnlyList<ActivityListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
