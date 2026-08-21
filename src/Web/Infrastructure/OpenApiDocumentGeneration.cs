using System.Reflection;

namespace Cane360.Web.Infrastructure;

public static class OpenApiDocumentGeneration
{
    private const string HostAssemblyName = "GetDocument.Insider";

    public static bool IsRequested() => IsRequested(Assembly.GetEntryAssembly()?.GetName().Name);

    public static bool IsRequested(string? entryAssemblyName) =>
        string.Equals(entryAssemblyName, HostAssemblyName, StringComparison.Ordinal);
}
