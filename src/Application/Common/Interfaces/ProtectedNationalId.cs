namespace Cane360.Application.Common.Interfaces;

public sealed record ProtectedNationalId(
    byte[] Ciphertext,
    byte[] Nonce,
    byte[] Tag,
    string KeyId,
    byte[] FarmScopedFingerprint,
    string DisplayMask);
