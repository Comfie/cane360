using System.Globalization;

namespace Cane360.Application.Common.Models;

/// <summary>CSV encoding keeps untrusted text distinct from authoritative numeric values.</summary>
public static class CsvCell
{
    public static string Text(string? value)
    {
        string text = value ?? string.Empty;
        ReadOnlySpan<char> significant = text.AsSpan().TrimStart();
        if ((!significant.IsEmpty && significant[0] is '=' or '+' or '-' or '@') ||
            (text.Length > 0 && text[0] is '\t' or '\r' or '\n'))
        {
            text = "'" + text;
        }

        return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    public static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
