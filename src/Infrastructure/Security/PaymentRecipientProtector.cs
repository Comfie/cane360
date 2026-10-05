using System.Security.Cryptography;
using System.Text;
using Cane360.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Cane360.Infrastructure.Security;

public sealed class PaymentRecipientProtector(IConfiguration configuration) : IPaymentRecipientProtector
{
    private const string Section = "Cane360Security:NationalId";

    public ProtectedPaymentRecipient Protect(Guid tenantId, Guid farmId, Guid paymentId, string recipientNumber)
    {
        string digits = new((recipientNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length is < 4 or > 20)
        {
            throw new ArgumentException("A valid recipient number is required.");
        }

        string keyId = configuration[$"{Section}:ActiveKeyId"] ??
                       throw new InvalidOperationException("Sensitive-data protection is not configured.");
        string encoded = configuration[$"{Section}:Keys:{keyId}"] ??
                         throw new InvalidOperationException("Sensitive-data protection is not configured.");
        byte[] key = Convert.FromBase64String(encoded);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Sensitive-data protection keys must be 256-bit values.");
        }

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] tag = new byte[16];
        byte[] plaintext = Encoding.UTF8.GetBytes(digits);
        byte[] ciphertext = new byte[plaintext.Length];
        using (AesGcm aes = new(key, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag,
                Encoding.UTF8.GetBytes($"cane360:payment-recipient:v1:{tenantId:N}:{farmId:N}:{paymentId:N}"));
        }

        return new ProtectedPaymentRecipient(ciphertext, nonce, tag, keyId, $"•••• {digits[^4..]}");
    }
}
