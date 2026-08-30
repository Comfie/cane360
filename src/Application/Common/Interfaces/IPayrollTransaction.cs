namespace Cane360.Application.Common.Interfaces;

public interface IPayrollTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
