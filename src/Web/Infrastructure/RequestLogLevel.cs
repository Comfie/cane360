using Serilog.Events;

namespace Cane360.Web.Infrastructure;

public static class RequestLogLevel
{
    public static LogEventLevel Select(int statusCode, double elapsedMilliseconds, Exception? exception) =>
        exception is not null || statusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden or StatusCodes.Status409Conflict || elapsedMilliseconds > 500
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
}
