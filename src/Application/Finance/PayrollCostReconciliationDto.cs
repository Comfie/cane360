namespace Cane360.Application.Finance;

public sealed record PayrollCostReconciliationDto(int ApprovedEarningSourcesExamined,
    int PostingsAdded, int ExistingPostingsPreserved);
