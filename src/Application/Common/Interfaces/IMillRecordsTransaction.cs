namespace Cane360.Application.Common.Interfaces;

public interface IMillRecordsTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
