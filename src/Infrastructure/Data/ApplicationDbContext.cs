using System.Reflection;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;
using Cane360.Domain.Inventory;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<GrowerProfile> GrowerProfiles => Set<GrowerProfile>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<CropVariety> CropVarieties => Set<CropVariety>();
    public DbSet<CropCycle> CropCycles => Set<CropCycle>();
    public DbSet<HarvestResult> HarvestResults => Set<HarvestResult>();
    public DbSet<CropCycleStatusChange> CropCycleStatusChanges => Set<CropCycleStatusChange>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonRoleAssignment> PersonRoleAssignments => Set<PersonRoleAssignment>();
    public DbSet<FieldLineProfile> FieldLineProfiles => Set<FieldLineProfile>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityStatusChange> ActivityStatusChanges => Set<ActivityStatusChange>();
    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();
    public DbSet<WorkerProfile> WorkerProfiles => Set<WorkerProfile>();
    public DbSet<WorkerRate> WorkerRates => Set<WorkerRate>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<WorkRecord> WorkRecords => Set<WorkRecord>();
    public DbSet<WorkRecordActivity> WorkRecordActivities => Set<WorkRecordActivity>();
    public DbSet<WorkScope> WorkScopes => Set<WorkScope>();
    public DbSet<WorkVerification> WorkVerifications => Set<WorkVerification>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptLine> StockReceiptLines => Set<StockReceiptLine>();
    public DbSet<StockPosition> StockPositions => Set<StockPosition>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<InventoryLeakageExport> InventoryLeakageExports => Set<InventoryLeakageExport>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<CorrectionRecord> CorrectionRecords => Set<CorrectionRecord>();
    public DbSet<InventoryAuditEventLink> InventoryAuditEventLinks => Set<InventoryAuditEventLink>();
    public DbSet<InventoryApplicationRule> InventoryApplicationRules => Set<InventoryApplicationRule>();
    public DbSet<InputRequest> InputRequests => Set<InputRequest>();
    public DbSet<InputRequestLine> InputRequestLines => Set<InputRequestLine>();
    public DbSet<StockIssue> StockIssues => Set<StockIssue>();
    public DbSet<StockIssueLine> StockIssueLines => Set<StockIssueLine>();
    public DbSet<FieldReceipt> FieldReceipts => Set<FieldReceipt>();
    public DbSet<FieldReceiptLine> FieldReceiptLines => Set<FieldReceiptLine>();
    public DbSet<InputApplication> InputApplications => Set<InputApplication>();
    public DbSet<InputApplicationLine> InputApplicationLines => Set<InputApplicationLine>();
    public DbSet<StockReturn> StockReturns => Set<StockReturn>();
    public DbSet<StockReturnLine> StockReturnLines => Set<StockReturnLine>();
    public DbSet<InventoryLoss> InventoryLosses => Set<InventoryLoss>();
    public DbSet<OperationalCostPosting> OperationalCostPostings => Set<OperationalCostPosting>();
    public DbSet<ControlException> ControlExceptions => Set<ControlException>();
    public DbSet<FieldAccountabilityCorrection> FieldAccountabilityCorrections => Set<FieldAccountabilityCorrection>();
    public DbSet<ManagerInvitation> ManagerInvitations => Set<ManagerInvitation>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<WorkerAdvance> WorkerAdvances => Set<WorkerAdvance>();
    public DbSet<AdvanceInstallment> AdvanceInstallments => Set<AdvanceInstallment>();
    public DbSet<AdvanceApproval> AdvanceApprovals => Set<AdvanceApproval>();
    public DbSet<AdvanceIssue> AdvanceIssues => Set<AdvanceIssue>();
    public DbSet<PayrollAuditEventLink> PayrollAuditEventLinks => Set<PayrollAuditEventLink>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollCalculation> PayrollCalculations => Set<PayrollCalculation>();
    public DbSet<PayrollWorkerLine> PayrollWorkerLines => Set<PayrollWorkerLine>();
    public DbSet<PayrollEarningLine> PayrollEarningLines => Set<PayrollEarningLine>();
    public DbSet<PayrollAdvanceDeduction> PayrollAdvanceDeductions => Set<PayrollAdvanceDeduction>();
    public DbSet<PayrollApproval> PayrollApprovals => Set<PayrollApproval>();
    public DbSet<PayrollEvidenceConsumption> PayrollEvidenceConsumptions => Set<PayrollEvidenceConsumption>();
    public DbSet<AdvanceRecovery> AdvanceRecoveries => Set<AdvanceRecovery>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("btree_gist");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
