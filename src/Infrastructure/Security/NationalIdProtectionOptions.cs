using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Cane360.Infrastructure.Security;

public sealed class NationalIdProtectionOptions
{
    public const string SectionName = "Cane360Security:NationalId";

    public string ActiveKeyId { get; init; } = string.Empty;

    public Dictionary<string, string> Keys { get; init; } = [];

    public string FingerprintKey { get; init; } = string.Empty;
}
