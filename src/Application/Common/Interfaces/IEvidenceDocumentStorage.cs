namespace Cane360.Application.Common.Interfaces;

public interface IEvidenceDocumentStorage
{
    Task<StoredEvidence> SaveAsync(Stream source, string fileName, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
