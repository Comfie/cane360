using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record ActivityListItemDto(
    Guid Id,
    Guid FieldId,
    string FieldCode,
    string FieldName,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string ActivityTypeCode,
    string ActivityTypeName,
    string Kind,
    string? PlannedDate,
    string SupervisorName,
    string QuantityBasis,
    string? ActualAt,
    decimal? ActualQuantity,
    bool LineContextUnavailable,
    bool IsRetrospective,
    int EntryDelayDays,
    string? LateEntryReason,
    string Status,
    long Version,
    int SourceReferenceCount);
