namespace Cane360.Web.Models.Finance;

public sealed record PostOperationalTransactionRequest(long ExpectedVersion, string IdempotencyKey,
    bool AuthorizedClosedCycleCorrection = false, string? CorrectionReason = null);
