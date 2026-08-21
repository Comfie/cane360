using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record EvidenceLinkDto(
    Guid Id,
    string Role,
    string SourceSheetReference,
    string CapturedDate,
    string RecordedAt,
    string RecordedBy);
