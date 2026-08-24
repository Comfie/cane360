namespace Cane360.Application.Common.Interfaces;

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
