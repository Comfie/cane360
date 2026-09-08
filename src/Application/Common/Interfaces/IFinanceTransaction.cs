namespace Cane360.Application.Common.Interfaces;

public interface IFinanceTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
