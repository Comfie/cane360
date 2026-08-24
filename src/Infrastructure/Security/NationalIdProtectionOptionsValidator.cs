using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Cane360.Infrastructure.Security;

public sealed class NationalIdProtectionOptionsValidator : IValidateOptions<NationalIdProtectionOptions>
{
    public ValidateOptionsResult Validate(string? name, NationalIdProtectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ActiveKeyId))
        {
            return ValidateOptionsResult.Fail("National-ID protection active key ID is required.");
        }

        if (!options.Keys.TryGetValue(options.ActiveKeyId, out string? encryptionKey) ||
            string.IsNullOrWhiteSpace(encryptionKey))
        {
            return ValidateOptionsResult.Fail("National-ID protection active key ID does not resolve to a configured key.");
        }

        if (!TryDecodeKey(encryptionKey, out byte[] encryptionKeyBytes))
        {
            return ValidateOptionsResult.Fail("National-ID protection encryption key must be valid Base64 encoding exactly 32 bytes.");
        }

        if (!TryDecodeKey(options.FingerprintKey, out byte[] fingerprintKeyBytes))
        {
            return ValidateOptionsResult.Fail("National-ID protection fingerprint key must be valid Base64 encoding exactly 32 bytes.");
        }

        if (CryptographicOperations.FixedTimeEquals(encryptionKeyBytes, fingerprintKeyBytes))
        {
            return ValidateOptionsResult.Fail("National-ID protection encryption and fingerprint keys must be different.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool TryDecodeKey(string? encodedKey, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            return false;
        }

        try
        {
            key = Convert.FromBase64String(encodedKey);
            return key.Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
