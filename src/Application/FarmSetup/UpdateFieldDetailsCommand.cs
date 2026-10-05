namespace Cane360.Application.FarmSetup;

public sealed record UpdateFieldDetailsCommand(Guid FieldId, string Name, string IrrigationMethod,
    string? SoilNotes) : IRequest<FarmSetupDto>;
