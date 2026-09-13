namespace Cane360.Web.Models.Finance;

public sealed record ApproveBudgetRequest(long ExpectedRowVersion, string IdempotencyKey);
