namespace Cane360.Application.Payroll;

public sealed record GetPayrollPreflightQuery(Guid PayrollPeriodId, Guid? WorkerId, bool? Eligible, string? EvidenceType, int Page, int PageSize) : IRequest<PayrollPreflightDto>;
