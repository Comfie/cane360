using Cane360.Domain.Farms;

namespace Cane360.Application.Administration;

public static class AdministrationCapabilities
{
    public const string ViewFarmData = "View farm and report data";
    public const string ConfigureReferences = "Configure reference data";
    public const string CaptureOperations = "Capture permitted operational records";
    public const string NormalInputApproval = "Normal input approval";
    public const string EscalatedInputApproval = "Escalated input approval";
    public const string StockAdjustmentApproval = "Stock adjustment approval";
    public const string PayrollPreparation = "Payroll preparation and submission";
    public const string PayrollApproval = "Payroll approval";
    public const string RecordPayrollPayment = "Record payroll payment";
    public const string UserAdministration = "User administration";
    public const string SensitiveReveal = "Sensitive-data reveal";
    public const string AuditAccess = "Tenant audit access";

    public static bool Can(string role, string capability) => capability switch
    {
        ViewFarmData or ConfigureReferences or CaptureOperations or NormalInputApproval or
            RecordPayrollPayment =>
            role is TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager,
        EscalatedInputApproval or StockAdjustmentApproval or PayrollApproval or
            UserAdministration or SensitiveReveal or AuditAccess =>
            role == TenantSecurityRoles.Grower,
        PayrollPreparation => role == TenantSecurityRoles.FarmManager,
        _ => false
    };

    public static IReadOnlyList<AdministrationCapabilityDto> Matrix()
    {
        string[] capabilities = [ViewFarmData, ConfigureReferences, CaptureOperations, NormalInputApproval,
            EscalatedInputApproval, StockAdjustmentApproval, PayrollPreparation,
            PayrollApproval, RecordPayrollPayment, UserAdministration,
            SensitiveReveal, AuditAccess];
        return capabilities.Select(capability => new AdministrationCapabilityDto(capability,
            Can(TenantSecurityRoles.Grower, capability),
            Can(TenantSecurityRoles.FarmManager, capability), false)).ToArray();
    }
}
