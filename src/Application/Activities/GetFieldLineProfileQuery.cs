namespace Cane360.Application.Activities;

public sealed record GetFieldLineProfileQuery(Guid FieldId) : IRequest<FieldLineProfileDto?>;
