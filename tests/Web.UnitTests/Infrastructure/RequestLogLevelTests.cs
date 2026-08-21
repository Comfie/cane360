using Cane360.Web.Infrastructure;
using Serilog.Events;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Web.UnitTests.Infrastructure;

public class RequestLogLevelTests
{
    [TestCase(200, 100, LogEventLevel.Information)]
    [TestCase(400, 100, LogEventLevel.Information)]
    [TestCase(401, 100, LogEventLevel.Warning)]
    [TestCase(403, 100, LogEventLevel.Warning)]
    [TestCase(409, 100, LogEventLevel.Warning)]
    [TestCase(200, 501, LogEventLevel.Warning)]
    [TestCase(500, 100, LogEventLevel.Error)]
    public void SelectUsesOperationalSeverityForStatusAndDuration(int statusCode, double elapsedMilliseconds, LogEventLevel expected)
    {
        RequestLogLevel.Select(statusCode, elapsedMilliseconds, null).ShouldBe(expected);
    }

    [Test]
    public void SelectUsesErrorForUnhandledException()
    {
        RequestLogLevel.Select(200, 100, new InvalidOperationException()).ShouldBe(LogEventLevel.Error);
    }
}
