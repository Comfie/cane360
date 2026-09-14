namespace Cane360.Application.MillRecords;

public sealed record MillRecordsSessionDto(string Role, IReadOnlyList<MillFieldDto> Fields);
