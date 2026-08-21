using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record WorkRecordDto(
    Guid Id,
    Guid WorkerId,
    string WorkerName,
    Guid AttendanceId,
    Guid FieldId,
    string FieldName,
    DateOnly WorkDate,
    string PayBasis,
    decimal AppliedRateUsd,
    decimal? Quantity,
    decimal? CalculatedAmountUsd,
    string Status,
    IReadOnlyList<Guid> ActivityIds,
    IReadOnlyList<string> ActivityNames,
    IReadOnlyList<WorkScopeDto> Scopes,
    WorkVerificationDto? Verification,
    DateTimeOffset EnteredAt,
    int EntryDelayDays,
    Guid? CorrectsWorkRecordId,
    long Version);
