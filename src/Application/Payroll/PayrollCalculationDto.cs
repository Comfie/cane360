namespace Cane360.Application.Payroll;

public sealed record PayrollCalculationDto(Guid Id, int CalculationVersion, DateTimeOffset CalculatedAt, decimal GrossAmountUsd, decimal DeductionAmountUsd, decimal NetAmountUsd, int WorkerCount, int EvidenceCount, IReadOnlyList<string> BlockerCodes, int BlockerCount, string SourceFingerprint, IReadOnlyList<PayrollWorkerLineDto> Workers);
