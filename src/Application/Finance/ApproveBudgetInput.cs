namespace Cane360.Application.Finance;

public sealed record ApproveBudgetInput(long ExpectedRowVersion, string IdempotencyKey);
