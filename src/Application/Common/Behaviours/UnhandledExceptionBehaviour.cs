using Cane360.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cane360.Application.Common.Behaviours;

public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    private readonly IUser _user;

    public UnhandledExceptionBehaviour(ILogger<TRequest> logger, IUser user)
    {
        _logger = logger;
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (ConflictException)
        {
            _logger.LogInformation("Cane360 Request: Conflict for Request {Name} {CorrelationId}",
                typeof(TRequest).Name, _user.CorrelationId);

            throw;
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogError(ex, "Cane360 Request: Unhandled Exception for Request {Name} {CorrelationId}",
                requestName, _user.CorrelationId);

            throw;
        }
    }
}
