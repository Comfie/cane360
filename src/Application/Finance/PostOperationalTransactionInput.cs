namespace Cane360.Application.Finance;

public sealed record PostOperationalTransactionInput(long ExpectedVersion, string IdempotencyKey,
    bool AuthorizedClosedCycleCorrection = false, string? CorrectionReason = null);
