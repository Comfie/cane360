using MediatR;

namespace Cane360.Application.Session;

public sealed record GetSessionQuery : IRequest<SessionSummaryDto>;
