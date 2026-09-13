namespace Cane360.Application.Finance;

public sealed record BudgetDto(Guid Id, Guid CropCycleId, Guid FieldId, int Version, string Status,
    string Name, decimal? ReportingAreaHa, decimal? ExpectedProductionTonnes, string? Notes,
    string CreatedByUserId, DateTimeOffset CreatedAt, string? SubmittedByUserId,
    DateTimeOffset? SubmittedAt, string? ApprovedByUserId, DateTimeOffset? ApprovedAt,
    Guid? SupersedesBudgetId, long RowVersion, decimal LabourBudgetUsd,
    decimal AppliedInputBudgetUsd, decimal DirectExpenseBudgetUsd,
    decimal ApprovedVarianceBudgetUsd, decimal TotalBudgetUsd,
    IReadOnlyList<BudgetLineDto> Lines);
