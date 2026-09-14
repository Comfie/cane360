namespace Cane360.Web.Models.MillRecords;

public sealed record EvidenceUploadRequest(string FileName, string ContentType,
    string ContentBase64);
