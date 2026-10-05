namespace Cane360.Application.Common.Interfaces;

public interface IInventoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
