namespace Cane360.Application.Common.Interfaces;

public sealed record ProtectedPaymentRecipient(byte[] Ciphertext, byte[] Nonce, byte[] Tag,
    string KeyId, string DisplayMask);
