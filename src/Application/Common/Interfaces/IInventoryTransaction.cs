using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Common.Interfaces;

public interface IInventoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
