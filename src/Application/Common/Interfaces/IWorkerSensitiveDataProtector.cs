namespace Cane360.Application.Common.Interfaces;

public sealed record ProtectedNationalId(
    byte[] Ciphertext,
    byte[] Nonce,
    byte[] Tag,
    string KeyId,
    byte[] FarmScopedFingerprint,
    string DisplayMask);

public interface IWorkerSensitiveDataProtector
{
    ProtectedNationalId Protect(Guid tenantId, Guid farmId, Guid workerId, string nationalId);

    string Reveal(
        Guid tenantId,
        Guid farmId,
        Guid workerId,
        byte[] ciphertext,
        byte[] nonce,
        byte[] tag,
        string keyId);
}
