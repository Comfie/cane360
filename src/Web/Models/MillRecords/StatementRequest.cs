namespace Cane360.Web.Models.MillRecords;

public sealed record StatementRequest(Guid MillId, string StatementReference,
    string PeriodStart, string PeriodEnd, decimal TotalTonnes, decimal TotalAmountUsd,
    string? Notes, long ExpectedVersion);
