namespace Cane360.Application.Common.Interfaces;

public interface IPaymentRecipientProtector
{
    ProtectedPaymentRecipient Protect(Guid tenantId, Guid farmId, Guid paymentId, string recipientNumber);
}
