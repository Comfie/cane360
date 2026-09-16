namespace Cane360.Web.Models.Administration;

public sealed record CreateDocumentCategoryRequest(string Code, string Name,
    string? Description);
