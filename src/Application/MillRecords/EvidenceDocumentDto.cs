namespace Cane360.Application.MillRecords;

public sealed record EvidenceDocumentDto(Guid Id, string OriginalFileName, string ContentType,
    long SizeBytes, DateTimeOffset UploadedAt);
