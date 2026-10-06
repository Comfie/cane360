using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize]
public sealed record GetInventoryCategoriesQuery : IRequest<IReadOnlyList<InventoryCategoryDto>>;
