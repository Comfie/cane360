using System.Globalization;

namespace Cane360.Web.Infrastructure;

public static class TransportValueParser
{
    private const string DateFormat = "yyyy-MM-dd";

    public static bool TryParseDateOnly(string? value, out DateOnly result) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    public static bool TryParseOptionalDateOnly(string? value, out DateOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseDateOnly(value, out var parsed))
        {
            return false;
        }

        result = parsed;
        return true;
    }

    public static bool TryParseOffsetTimestamp(string? value, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value) || !HasExplicitOffset(value))
        {
            return false;
        }

        return DateTimeOffset.TryParseExact(
            value,
            ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", "yyyy-MM-dd'T'HH:mmK"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool HasExplicitOffset(string value)
    {
        if (value.EndsWith('Z'))
        {
            return true;
        }

        if (value.Length < 6)
        {
            return false;
        }

        var suffix = value.AsSpan(value.Length - 6);
        return (suffix[0] is '+' or '-') &&
            char.IsAsciiDigit(suffix[1]) &&
            char.IsAsciiDigit(suffix[2]) &&
            suffix[3] == ':' &&
            char.IsAsciiDigit(suffix[4]) &&
            char.IsAsciiDigit(suffix[5]);
    }
}
