using Cane360.Application.Payroll;

namespace Cane360.Application.Common.Interfaces;

public interface IPayrollSettlementService
{
    Task<RunSettlementDto> GetRunAsync(Guid runId, CancellationToken cancellationToken);
    Task<WorkerSettlementDto> GetWorkerAsync(Guid runId, int calculationVersion, Guid workerLineId, CancellationToken cancellationToken);
    Task<PayrollPaymentDto> RecordPaymentAsync(Guid runId, RecordPayrollPaymentInput input, CancellationToken cancellationToken);
    Task<PayrollPaymentDto> AcknowledgeAsync(Guid paymentId, RecordPaymentAcknowledgementInput input, CancellationToken cancellationToken);
    Task<PayrollPaymentDto> ReverseAsync(Guid paymentId, ReversePayrollPaymentInput input, CancellationToken cancellationToken);
    Task<RunSettlementDto> CloseAsync(Guid runId, ClosePayrollSettlementInput input, CancellationToken cancellationToken);
    Task<RunSettlementDto> ReopenAsync(Guid runId, ReopenPayrollSettlementInput input, CancellationToken cancellationToken);
    Task<OperationalPayslipDto> GetPayslipAsync(Guid runId, int calculationVersion, Guid workerLineId, CancellationToken cancellationToken);
    Task<CashPaymentRegisterDto> GetCashRegisterAsync(Guid runId, int calculationVersion, CancellationToken cancellationToken);
}
