namespace Cane360.Application.Payroll;

public sealed record PayrollEarningLineDto(Guid Id, Guid EvidenceId, string EvidenceType, DateOnly WorkDate, Guid AttendanceId, long AttendanceVersion, DateTimeOffset SupervisorVerifiedAt, DateTimeOffset ManagerConfirmedAt, Guid FieldId, IReadOnlyList<Guid> ActivityIds, decimal Quantity, string Unit, string RateType, decimal RateAmountUsd, Guid RateSourceId, long RateVersion, decimal EarningAmountUsd, string SourceFingerprint);
