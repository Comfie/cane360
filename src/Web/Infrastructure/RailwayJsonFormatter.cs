using System.Globalization;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Cane360.Web.Infrastructure;

public sealed class RailwayJsonFormatter : ITextFormatter
{
    private static readonly JsonValueFormatter ValueFormatter = new(typeTagName: null);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        output.Write('{');
        WriteString(output, "timestamp", logEvent.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        output.Write(',');
        WriteString(output, "level", MapLevel(logEvent.Level));
        output.Write(',');
        WriteString(output, "message", logEvent.RenderMessage(CultureInfo.InvariantCulture));

        if (logEvent.Exception is not null)
        {
            output.Write(',');
            WriteString(output, "exception", logEvent.Exception.ToString());
        }

        foreach ((string name, LogEventPropertyValue value) in logEvent.Properties.OrderBy(property => property.Key, StringComparer.Ordinal))
        {
            if (name is "timestamp" or "level" or "message" or "exception")
            {
                continue;
            }

            output.Write(',');
            output.Write(JsonSerializer.Serialize(name));
            output.Write(':');
            ValueFormatter.Format(value, output);
        }

        output.WriteLine('}');
    }

    private static void WriteString(TextWriter output, string name, string value)
    {
        output.Write(JsonSerializer.Serialize(name));
        output.Write(':');
        output.Write(JsonSerializer.Serialize(value));
    }

    private static string MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "debug",
        LogEventLevel.Debug => "debug",
        LogEventLevel.Information => "info",
        LogEventLevel.Warning => "warn",
        LogEventLevel.Error => "error",
        LogEventLevel.Fatal => "error",
        _ => "info"
    };
}
