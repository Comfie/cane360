using System.Text.Json;
using Cane360.Web.Infrastructure;
using Serilog.Events;
using Serilog.Parsing;

namespace Cane360.Web.UnitTests.Infrastructure;

public class RailwayJsonFormatterTests
{
    [TestCase(LogEventLevel.Information, "info")]
    [TestCase(LogEventLevel.Warning, "warn")]
    [TestCase(LogEventLevel.Fatal, "error")]
    public void FormatProducesRailwayFieldsAndSingleLineJson(LogEventLevel eventLevel, string expectedLevel)
    {
        var logEvent = new LogEvent(
            DateTimeOffset.Parse("2026-09-16T06:00:00Z"),
            eventLevel,
            null,
            new MessageTemplateParser().Parse("Request {EndpointRoute}"),
            [new LogEventProperty("EndpointRoute", new ScalarValue("/api/Health/ready"))]);
        var output = new StringWriter();

        new RailwayJsonFormatter().Format(logEvent, output);

        string rendered = output.ToString();
        rendered.Count(value => value == '\n').ShouldBe(1);
        using JsonDocument json = JsonDocument.Parse(rendered);
        json.RootElement.GetProperty("level").GetString().ShouldBe(expectedLevel);
        json.RootElement.GetProperty("message").GetString().ShouldBe("Request \"/api/Health/ready\"");
        json.RootElement.GetProperty("EndpointRoute").GetString().ShouldBe("/api/Health/ready");
    }

    [Test]
    public void FormatEscapesExceptionLinesWithoutBreakingJsonLine()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            new InvalidOperationException("first line\nsecond line"),
            new MessageTemplateParser().Parse("Failure"),
            []);
        var output = new StringWriter();

        new RailwayJsonFormatter().Format(logEvent, output);

        string rendered = output.ToString();
        rendered.Count(value => value == '\n').ShouldBe(1);
        using JsonDocument json = JsonDocument.Parse(rendered);
        string exception = json.RootElement.GetProperty("exception").GetString() ?? string.Empty;
        exception.ShouldContain("second line");
    }
}
