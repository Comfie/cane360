using System.Reflection;

namespace Cane360.Web.Infrastructure;

public static class OpenApiDocumentGeneration
{
    private const string HostAssemblyName = "GetDocument.Insider";

    public static bool IsRequested()
    {
        return IsRequested(Assembly.GetEntryAssembly()?.GetName().Name);
    }

    public static bool IsRequested(string? entryAssemblyName)
    {
        return string.Equals(entryAssemblyName, HostAssemblyName, StringComparison.Ordinal);
    }
}
