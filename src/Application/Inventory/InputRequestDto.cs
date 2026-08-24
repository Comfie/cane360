namespace Cane360.Application.Inventory;

public sealed record InputRequestDto(
    Guid Id, Guid FieldId, Guid CropCycleId, Guid ActivityId, DateOnly OperationalDate,
    string ActivityTypeName, string FieldName, string Status, bool RequiresGrower,
    long Version, IReadOnlyList<InputRequestLineDto> Lines);
