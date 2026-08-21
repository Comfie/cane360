using System.Security.Cryptography;
using System.Text;
using Cane360.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Cane360.Infrastructure.Security;

public sealed class WorkerSensitiveDataProtector(IConfiguration configuration)
    : IWorkerSensitiveDataProtector
{
    private const string ConfigurationSection = "Cane360Security:NationalId";

    public ProtectedNationalId Protect(Guid tenantId, Guid farmId, Guid workerId, string nationalId)
    {
        var normalized = Normalize(nationalId);
        var (keyId, key) = ActiveEncryptionKey();
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var plaintext = Encoding.UTF8.GetBytes(normalized);
        var ciphertext = new byte[plaintext.Length];
        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData(tenantId, farmId, workerId));
        }

        return new ProtectedNationalId(
            ciphertext,
            nonce,
            tag,
            keyId,
            Fingerprint(farmId, normalized),
            Mask(normalized));
    }

    public string Reveal(
        Guid tenantId,
        Guid farmId,
        Guid workerId,
        byte[] ciphertext,
        byte[] nonce,
        byte[] tag,
        string keyId)
    {
        var key = EncryptionKey(keyId);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData(tenantId, farmId, workerId));
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    private (string KeyId, byte[] Key) ActiveEncryptionKey()
    {
        var keyId = configuration[$"{ConfigurationSection}:ActiveKeyId"];
        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new InvalidOperationException("National-ID protection is not configured.");
        }

        return (keyId, EncryptionKey(keyId));
    }

    private byte[] EncryptionKey(string keyId) => ReadKey($"{ConfigurationSection}:Keys:{keyId}");

    private byte[] Fingerprint(Guid farmId, string normalized)
    {
        using var hmac = new HMACSHA256(ReadKey($"{ConfigurationSection}:FingerprintKey"));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes($"{farmId:N}:{normalized}"));
    }

    private byte[] ReadKey(string path)
    {
        var encoded = configuration[path];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            throw new InvalidOperationException("National-ID protection is not configured.");
        }

        try
        {
            var key = Convert.FromBase64String(encoded);
            if (key.Length != 32)
            {
                throw new InvalidOperationException("National-ID protection keys must be 256-bit values.");
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("National-ID protection configuration is invalid.", exception);
        }
    }

    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        if (normalized.Length < 4 || normalized.Length > 40)
        {
            throw new ArgumentException("National ID must contain between 4 and 40 letters or digits.");
        }

        return normalized;
    }

    private static string Mask(string normalized) => $"••••••{normalized[^2..]}";

    private static byte[] AssociatedData(Guid tenantId, Guid farmId, Guid workerId) =>
        Encoding.UTF8.GetBytes($"cane360:national-id:v1:{tenantId:N}:{farmId:N}:{workerId:N}");
}
