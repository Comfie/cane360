namespace Cane360.Web.Models.Administration;

public sealed record UpdateDocumentCategoryRequest(string Name, string? Description,
    long ExpectedVersion);
