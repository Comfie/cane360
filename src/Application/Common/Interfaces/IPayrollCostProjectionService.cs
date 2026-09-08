using Cane360.Application.Finance;

namespace Cane360.Application.Common.Interfaces;

public interface IPayrollCostProjectionService
{
    Task<PayrollCostReconciliationDto> ProjectAsync(Tenant tenant, Farm farm, PayrollRun run,
        PayrollCalculation calculation, IUser user, CancellationToken cancellationToken);
    Task<PayrollCostReconciliationDto> ReconcileAsync(Tenant tenant, Farm farm, IUser user,
        CancellationToken cancellationToken);
}
