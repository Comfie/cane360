namespace Cane360.Application.Administration;

public sealed record AdministrationAuditPageDto(
    int Page, int PageSize, int TotalCount, IReadOnlyList<AdministrationAuditDto> Items);
