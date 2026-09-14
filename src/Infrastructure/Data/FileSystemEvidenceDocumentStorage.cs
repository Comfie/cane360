using Cane360.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cane360.Infrastructure.Data;

public sealed class FileSystemEvidenceDocumentStorage : IEvidenceDocumentStorage
{
    private readonly string _root;

    public FileSystemEvidenceDocumentStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        string configured = configuration["EvidenceStorage:RootPath"] ??
            Path.Combine(environment.ContentRootPath, "App_Data", "evidence");
        _root = Path.GetFullPath(configured);
    }

    public async Task<StoredEvidence> SaveAsync(Stream source, string fileName,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        string storageKey = Guid.NewGuid().ToString("N");
        string path = Resolve(storageKey);
        await using FileStream destination = new(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await source.CopyToAsync(destination, cancellationToken);
        await destination.FlushAsync(cancellationToken);
        return new(storageKey, destination.Length);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(Resolve(storageKey), FileMode.Open, FileAccess.Read,
            FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    private string Resolve(string storageKey)
    {
        if (storageKey.Length != 32 || storageKey.Any(x => !char.IsAsciiHexDigit(x)))
            throw new InvalidOperationException("The evidence storage identity is invalid.");
        string path = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("The evidence storage identity is invalid.");
        return path;
    }
}
