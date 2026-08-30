namespace Cane360.Application.Payroll;

public sealed record GetPayrollCalculationQuery(Guid PayrollRunId, int CalculationVersion) : IRequest<PayrollCalculationDto>;
