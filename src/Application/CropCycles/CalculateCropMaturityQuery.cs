namespace Cane360.Application.CropCycles;

public sealed record CalculateCropMaturityQuery(Guid FieldId, DateOnly? PlantingDate) : IRequest<CropMaturityDto>;
