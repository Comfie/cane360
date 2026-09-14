namespace Cane360.Application.MillRecords;

public sealed record EvidenceDownload(Stream Content, string FileName, string ContentType);
