using Cane360.Application.Common.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace Cane360.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly IUser _user;

    public LoggingBehaviour(ILogger<TRequest> logger, IUser user)
    {
        _logger = logger;
        _user = user;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Cane360 application request {RequestName}; reference {CorrelationId}",
            requestName, _user.CorrelationId);

        return Task.CompletedTask;
    }
}
