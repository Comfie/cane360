namespace Cane360.Application.Labour;

public sealed record RevealWorkerNationalIdCommand(Guid WorkerId, string Reason) : IRequest<RevealedNationalIdDto>;
