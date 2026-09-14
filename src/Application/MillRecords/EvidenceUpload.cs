namespace Cane360.Application.MillRecords;

public sealed record EvidenceUpload(Stream Content, string FileName, string ContentType, long Length);
