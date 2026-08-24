using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddInputRequestsApprovalsAndIssues is approved and applied to the separately configured PostgreSQL integration database.")]
[Category("Phase5BPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlInputIssueAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;
    private string _growerUserId = string.Empty;
    private Guid _tenantId;
    private Guid _farmId;
    private Guid _storeId;
    private Guid _activityTypeId;
    private Guid _activityId;
    private Guid _fieldId;
    private Guid _cycleId;
    private Guid _issuerId;
    private Guid _recipientId;

    [OneTimeSetUp]
    public async Task EstablishIsolatedSyntheticTenant()
    {
        Environment.GetEnvironmentVariable("CANE360_INTEGRATION_TARGET")
            .ShouldBe("DedicatedTestDatabase", "the repository rules prohibit destructive or concurrency tests against Railway development");
        _connectionString = Environment.GetEnvironmentVariable("CANE360_INTEGRATION_DB")
            ?? throw new InvalidOperationException("CANE360_INTEGRATION_DB is not configured.");
        _runId = $"AUTOTEST-P5B-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _growerUserId = $"p5b-grower-{Guid.NewGuid():N}";
        var tenant = Tenant.CreateForGrower(_growerUserId, _runId, null);
        var variety = tenant.AddCropVariety($"V{Guid.NewGuid():N}"[..20], "Synthetic N14");
        var type = tenant.AddActivityType($"A{Guid.NewGuid():N}"[..20], "Synthetic input work", true, true,
            ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm($"F{Guid.NewGuid():N}"[..20], _runId,
            "Synthetic address", "Integration database", "Synthetic", 10m, "Synthetic");
        var supervisor = farm.AddPerson($"{_runId} supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, true, new DateOnly(2026, 1, 1));
        var issuer = farm.AddPerson($"{_runId} storekeeper", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(issuer, PersonRole.Storekeeper, true, new DateOnly(2026, 1, 1));
        var recipient = farm.AddPerson($"{_runId} recipient", null, new DateOnly(2026, 1, 1));
        var field = farm.AddField("P5B-A", "Synthetic block", 10m, null,
            ReportingAreaSource.Declared, "Synthetic", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31),
            500m, DateTimeOffset.UtcNow, _growerUserId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, _growerUserId);
        var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type,
            ActivityPlanningKind.Planned, InventoryAccessDate, supervisor.Id);
        _tenantId = tenant.Id;
        _farmId = farm.Id;
        _storeId = farm.Store.Id;
        _activityTypeId = type.Id;
        _activityId = activity.Id;
        _fieldId = field.Id;
        _cycleId = cycle.Id;
        _issuerId = issuer.Id;
        _recipientId = recipient.Id;
        TestContext.Progress.WriteLine($"Retained synthetic Phase 5B run: {_runId}; tenant: {_tenantId}");
        await using var context = CreateContext();
        context.Users.Add(User(_growerUserId));
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task OverlappingEffectiveRulesAreRejectedByPostgreSql()
    {
        var identity = await CreateStockIdentityAsync(100m, 3m);
        await using var firstContext = CreateContext();
        var item = await firstContext.InventoryItems.SingleAsync(value => value.Id == identity.ItemId);
        firstContext.InventoryApplicationRules.Add(InventoryApplicationRule.Create(_tenantId, _farmId,
            item, _activityTypeId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            ApplicationCoverageBasis.FieldReportingHectares, 10m, 5m, 10m));
        await firstContext.SaveChangesAsync();
        await using var overlapContext = CreateContext();
        item = await overlapContext.InventoryItems.SingleAsync(value => value.Id == identity.ItemId);
        overlapContext.InventoryApplicationRules.Add(InventoryApplicationRule.Create(_tenantId, _farmId,
            item, _activityTypeId, new DateOnly(2026, 6, 1), null,
            ApplicationCoverageBasis.FieldReportingHectares, 11m, 5m, 10m));

        var error = await Should.ThrowAsync<DbUpdateException>(() => overlapContext.SaveChangesAsync());
        ((PostgresException)error.InnerException!).ConstraintName.ShouldBe("EX_InventoryApplicationRules_NoOverlap");
    }

    [Test]
    public async Task ConcurrentOversubscribedIssuesAllowExactlyOnePosting()
    {
        var identity = await CreateApprovedRequestAsync(100m, 3m, 100m);
        var firstIssue = await CreateIssueDraftAsync(identity, 70m);
        var secondIssue = await CreateIssueDraftAsync(identity, 70m);

        var results = await Task.WhenAll(PostResultAsync(firstIssue), PostResultAsync(secondIssue));

        results.Count(result => result).ShouldBe(1);
        await using var context = CreateContext();
        var movementCount = await context.StockMovements.CountAsync(movement =>
            movement.TenantId == _tenantId && movement.StockIssueLineId.HasValue &&
            (movement.StockIssueLineId == firstIssue.LineId || movement.StockIssueLineId == secondIssue.LineId));
        movementCount.ShouldBe(1);
    }

    [Test]
    public async Task IssueRetryIsIdempotentAndSnapshotsPostingOrderMovingAverage()
    {
        var identity = await CreateApprovedRequestAsync(100m, 4m, 50m);
        var issue = await CreateIssueDraftAsync(identity, 20m);
        var command = new PostStockIssueCommand(issue.IssueId, issue.Version,
            $"{_runId}-issue-retry-{Guid.NewGuid():N}");
        await PostAsync(command);
        await PostAsync(command);
        await using var context = CreateContext();
        var line = await context.StockIssueLines.SingleAsync(value => value.Id == issue.LineId);
        var movementCount = await context.StockMovements.CountAsync(value =>
            value.TenantId == _tenantId && value.StockIssueLineId == issue.LineId);

        movementCount.ShouldBe(1);
        line.IssueUnitCostUsd.ShouldBe(4m);
        line.IssueValueUsd.ShouldBe(80m);
    }

    [Test]
    public async Task DraftIssueCreatesNoMovementAndTenantQueriesRemainIsolated()
    {
        var identity = await CreateApprovedRequestAsync(20m, 2m, 10m);
        var issue = await CreateIssueDraftAsync(identity, 5m);
        await using var context = CreateContext();
        (await context.StockMovements.AnyAsync(value => value.StockIssueLineId == issue.LineId)).ShouldBeFalse();
        var repository = new InventoryRepository(context);
        (await repository.GetStockIssuesAsync(Guid.NewGuid(), _farmId, null, false,
            CancellationToken.None)).ShouldBeEmpty();
    }

    [Test]
    public async Task ApprovalAndMovementRowsRemainAppendOnly()
    {
        var identity = await CreateApprovedRequestAsync(20m, 2m, 10m);
        var issue = await CreateIssueDraftAsync(identity, 5m);
        await PostAsync(new PostStockIssueCommand(issue.IssueId, issue.Version,
            $"{_runId}-append-{Guid.NewGuid():N}"));
        await using var context = CreateContext();
        var approvalId = await context.ApprovalDecisions.Where(value => value.InputRequestId == identity.RequestId)
            .Select(value => value.Id).SingleAsync();
        var movementId = await context.StockMovements.Where(value => value.StockIssueLineId == issue.LineId)
            .Select(value => value.Id).SingleAsync();

        await AssertAppendOnlyAsync($"UPDATE inventory.\"ApprovalDecisions\" SET \"Reason\" = 'tamper' WHERE \"Id\" = '{approvalId}' AND \"TenantId\" = '{_tenantId}'");
        await AssertAppendOnlyAsync($"DELETE FROM inventory.\"StockMovements\" WHERE \"Id\" = '{movementId}' AND \"TenantId\" = '{_tenantId}'");
    }

    private async Task<ApprovedRequestIdentity> CreateApprovedRequestAsync(
        decimal stockQuantity, decimal unitCost, decimal approvedQuantity)
    {
        var stock = await CreateStockIdentityAsync(stockQuantity, unitCost);
        await using var context = CreateContext();
        var item = await context.InventoryItems.SingleAsync(value => value.Id == stock.ItemId);
        var rule = InventoryApplicationRule.Create(_tenantId, _farmId, item, _activityTypeId,
            new DateOnly(2026, 1, 1), null, ApplicationCoverageBasis.FieldReportingHectares,
            approvedQuantity / 10m, 0m, 0m);
        var request = InputRequest.Create(_tenantId, _farmId, _fieldId, _cycleId, _activityId,
            InventoryAccessDate, _growerUserId);
        var line = request.AddLine(item, rule, 10m, approvedQuantity,
            stockQuantity, unitCost, request.Version);
        request.Submit(DateTimeOffset.UtcNow, $"{_runId}-submit-{Guid.NewGuid():N}", request.Version);
        request.OpenApproval(request.Version);
        var subjectVersion = request.Version;
        request.Decide(ApprovalOutcome.Approved, null, DateTimeOffset.UtcNow, request.Version);
        context.InventoryApplicationRules.Add(rule);
        context.InputRequests.Add(request);
        context.ApprovalDecisions.Add(ApprovalDecision.CreateInputRequestDecision(_tenantId, _farmId,
            request.Id, subjectVersion, ApprovalOutcome.Approved, _growerUserId,
            TenantSecurityRoles.Grower, DateTimeOffset.UtcNow, null,
            $"{_runId}-approve-{Guid.NewGuid():N}"));
        await context.SaveChangesAsync();
        return new(request.Id, line.Id, stock.ItemId, stock.PositionId, approvedQuantity);
    }

    private async Task<StockIdentity> CreateStockIdentityAsync(decimal quantity, decimal unitCost)
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid().ToString("N");
        var unit = UnitOfMeasure.Create(_tenantId, $"U{token}"[..20], "Synthetic unit", "Mass", 6);
        var item = InventoryItem.Create(_tenantId, _farmId, $"I{token}"[..20], $"{_runId} item",
            InventoryItemCategory.Other, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var supplier = Supplier.Create(_tenantId, _farmId, $"S{token}"[..20], $"{_runId} supplier", null);
        var position = StockPosition.Create(_tenantId, _farmId, _storeId, item.Id, null);
        var receipt = StockReceipt.Create(_tenantId, _farmId, _storeId, StockReceiptType.Purchase,
            supplier.Id, InventoryAccessDate, null, $"{_runId}-receipt-{token}", null, null, 0);
        var line = receipt.AddLine(item, null, quantity, unitCost, receipt.Version);
        receipt.MarkPosted(DateTimeOffset.UtcNow, _growerUserId,
            $"{_runId}-receipt-post-{token}", receipt.Version);
        var movement = StockMovement.CreateReceipt(_tenantId, _farmId, _storeId, position.Id,
            line, StockReceiptType.Purchase, InventoryAccessDate, DateTimeOffset.UtcNow,
            _growerUserId, null, $"receipt:{line.Id:N}:posted");
        context.UnitOfMeasures.Add(unit);
        context.InventoryItems.Add(item);
        context.Suppliers.Add(supplier);
        context.StockPositions.Add(position);
        context.StockReceipts.Add(receipt);
        context.StockMovements.Add(movement);
        await context.SaveChangesAsync();
        return new(item.Id, position.Id);
    }

    private async Task<IssueIdentity> CreateIssueDraftAsync(ApprovedRequestIdentity identity, decimal quantity)
    {
        await using var context = CreateContext();
        var requestLine = await context.InputRequestLines.SingleAsync(value => value.Id == identity.RequestLineId);
        var issue = StockIssue.Create(_tenantId, _farmId, _storeId, identity.RequestId,
            InventoryAccessDate, _issuerId, _recipientId, null, 0);
        var line = issue.AddLine(requestLine, identity.PositionId, null, null, quantity, issue.Version);
        context.StockIssues.Add(issue);
        await context.SaveChangesAsync();
        return new(issue.Id, line.Id, issue.Version);
    }

    private async Task<bool> PostResultAsync(IssueIdentity issue)
    {
        try
        {
            await PostAsync(new PostStockIssueCommand(issue.IssueId, issue.Version,
                $"{_runId}-concurrent-{Guid.NewGuid():N}"));
            return true;
        }
        catch (ConflictException)
        {
            return false;
        }
    }

    private async Task PostAsync(PostStockIssueCommand command)
    {
        await using var context = CreateContext();
        var handler = new PostStockIssueCommandHandler(new FarmSetupRepository(context),
            new InventoryRepository(context), new AcceptanceUser(_growerUserId), TimeProvider.System);
        await handler.Handle(command, CancellationToken.None);
    }

    private async Task AssertAppendOnlyAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var exception = await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        exception.SqlState.ShouldBe("P0001");
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString).Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationUser User(string id) => new()
    {
        Id = id,
        UserName = $"{id}@invalid.example",
        NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(),
        Email = $"{id}@invalid.example",
        NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };

    private static DateOnly InventoryAccessDate => new(2026, 8, 22);

    private sealed class AcceptanceUser(string userId) : IUser
    {
        public string? Id => userId;
        public List<string>? Roles => null;
        public string? CorrelationId => $"p5b-acceptance-{Guid.NewGuid():N}";
    }

    private sealed record StockIdentity(Guid ItemId, Guid PositionId);
    private sealed record ApprovedRequestIdentity(Guid RequestId, Guid RequestLineId,
        Guid ItemId, Guid PositionId, decimal ApprovedQuantity);
    private sealed record IssueIdentity(Guid IssueId, Guid LineId, long Version);
}
