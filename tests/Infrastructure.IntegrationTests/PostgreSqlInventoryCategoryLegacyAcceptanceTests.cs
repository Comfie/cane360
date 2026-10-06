using System.Text.Json;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only against explicitly selected Railway Development for CR-01.4.")]
[NonParallelizable]
public sealed class PostgreSqlInventoryCategoryLegacyAcceptanceTests
{
    private const string FixturePath = "/private/tmp/cr014-legacy-fixture.json";

    [Test]
    [Category("CR014PreMigration")]
    public async Task PrepareLabelledLegacyItemAndReceiptBeforeMigration()
    {
        await using ApplicationDbContext context = Context();
        (await context.Database.GetAppliedMigrationsAsync()).ShouldNotContain(name => name.Contains("InventoryCategoryAdministration"));
        string label = $"AUTOTEST-CR014-LEGACY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        string userId = $"cr014-legacy-{Guid.NewGuid():N}";
        Tenant tenant = Tenant.CreateForGrower(userId, label, null);
        Farm farm = tenant.CreateFarm($"CR014-{Guid.NewGuid():N}"[..20], label, "Synthetic", "Railway", "Synthetic", 1m, "Synthetic");
        UnitOfMeasure unit = UnitOfMeasure.Create(tenant.Id, "KG", "Synthetic kilogram", "Mass", 6);
        InventoryItem item = InventoryItem.Create(tenant.Id, farm.Id, "LEGACY", label, InventoryItemCategory.Fertiliser,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        Supplier supplier = Supplier.Create(tenant.Id, farm.Id, "LEGACY", label, null);
        StockPosition position = StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null);
        StockReceipt receipt = StockReceipt.Create(tenant.Id, farm.Id, farm.Store.Id, StockReceiptType.Purchase, supplier.Id,
            new DateOnly(2026, 10, 6), null, label, null, null, 0);
        StockReceiptLine line = receipt.AddLine(item, null, 17m, 2.75m, receipt.Version);
        receipt.MarkPosted(DateTimeOffset.UtcNow, userId, label, receipt.Version);
        context.Users.Add(new ApplicationUser {Id = userId, UserName = label + "@example.invalid",
            NormalizedUserName = (label + "@example.invalid").ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")});
        context.Tenants.Add(tenant);
        // The database intentionally still has the legacy model. Only omit the newly added navigation from this fixture.
        context.ChangeTracker.DetectChanges();
        foreach (var entry in context.ChangeTracker.Entries<InventoryCategory>().ToArray()) entry.State = EntityState.Detached;
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.UnitOfMeasures.Add(unit); context.InventoryItems.Add(item); context.Suppliers.Add(supplier);
        context.StockPositions.Add(position); context.StockReceipts.Add(receipt);
        context.StockMovements.Add(StockMovement.CreateReceipt(tenant.Id, farm.Id, farm.Store.Id, position.Id, line,
            StockReceiptType.Purchase, receipt.ReceiptDate, DateTimeOffset.UtcNow, userId, null, label));
        await context.SaveChangesAsync();
        string snapshot = await Snapshot(context, tenant.Id);
        await File.WriteAllTextAsync(FixturePath, JsonSerializer.Serialize(new LegacyFixture(tenant.Id, farm.Id, label, snapshot)));
        TestContext.Progress.WriteLine($"Retained synthetic CR-01.4 legacy fixture: {label}; tenant {tenant.Id}");
    }

    [Test]
    [Category("CR014PostMigration")]
    public async Task MigratedLegacyCategoriesAndInventoryFactsRemainIdentical()
    {
        LegacyFixture fixture = JsonSerializer.Deserialize<LegacyFixture>(await File.ReadAllTextAsync(FixturePath))!;
        await using ApplicationDbContext context = Context();
        (await context.Tenants.Where(tenant => tenant.Id == fixture.TenantId)
            .Select(tenant => tenant.GrowerProfile.DisplayName).SingleAsync()).ShouldBe(fixture.Label);
        fixture.Label.ShouldStartWith("AUTOTEST-CR014-LEGACY-");
        (await Snapshot(context, fixture.TenantId)).ShouldBe(fixture.Snapshot);
        var categories = await context.InventoryCategories.AsNoTracking().Where(category => category.TenantId == fixture.TenantId)
            .OrderBy(category => category.DisplayOrder).ToArrayAsync();
        categories.Select(category => category.Code).ShouldBe(Enum.GetNames<InventoryItemCategory>());
        categories.Select(category => category.Name).ShouldBe(new[] {"Fertiliser", "Chemical", "Seed And Planting Material", "Other"});
        categories.ShouldAllBe(category => category.Active && category.Version == 1);
        InventoryItem item = await context.InventoryItems.AsNoTracking().SingleAsync(value => value.TenantId == fixture.TenantId);
        item.Category.ShouldBe("Fertiliser");
        var stock = await new InventoryRepository(context).GetStockOnHandAsync(fixture.TenantId, fixture.FarmId, default);
        stock.Single().Snapshot.Quantity.ShouldBe(17m);
        stock.Single().Snapshot.ValueUsd.ShouldBe(46.75m);
        stock.Single().Snapshot.WeightedAverageUnitCostUsd.ShouldBe(2.75m);
    }

    private static ApplicationDbContext Context()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("Development connection unavailable.")).Options);
    }

    private static async Task<string> Snapshot(ApplicationDbContext context, Guid tenantId)
    {
        Dictionary<string, string> facts = new();
        await context.Database.OpenConnectionAsync();
        foreach (string table in new[] {"InventoryItems", "StockPositions", "StockReceipts", "StockReceiptLines", "StockMovements"})
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT jsonb_agg(to_jsonb(row) ORDER BY row.\"Id\")::text FROM inventory.\"{table}\" row WHERE row.\"TenantId\" = @tenant";
            var parameter = command.CreateParameter(); parameter.ParameterName = "tenant"; parameter.Value = tenantId; command.Parameters.Add(parameter);
            facts.Add(table, (string)(await command.ExecuteScalarAsync())!);
        }
        return JsonSerializer.Serialize(facts);
    }

    private sealed record LegacyFixture(Guid TenantId, Guid FarmId, string Label, string Snapshot);
}
