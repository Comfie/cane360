namespace Cane360.Application.Payroll;

public sealed record PreflightEvidenceDto(Guid WorkerId, string WorkerName, Guid EvidenceId, string EvidenceType, DateOnly EventDate, Guid FieldId, string FieldName, string CropCycleName, IReadOnlyList<Guid> ActivityIds, IReadOnlyList<string> ActivityNames, decimal? Quantity, string QuantityOrAttendanceBasis, decimal AppliedRateUsd, string PayBasis, bool Eligible, IReadOnlyList<string> BlockerCodes, IReadOnlyList<string> BlockerExplanations, IReadOnlyList<PreflightSourceLinkDto> SourceChain);
