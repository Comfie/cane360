using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cane360.Web.UnitTests.Infrastructure;

public sealed class InventoryCategoryModelTests
{
    [Test]
    public void CategoryHasStablePrimaryKeyTenantUniquenessAndOptimisticConcurrency()
    {
        using ApplicationDbContext context = Context();
        IEntityType category = context.Model.FindEntityType(typeof(InventoryCategory))!;
        category.FindPrimaryKey()!.Properties.Select(property => property.Name).ShouldBe(new[] {"Id"});
        category.FindProperty("Version")!.IsConcurrencyToken.ShouldBeTrue();
        category.GetIndexes().Where(index => index.IsUnique).Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ShouldBe(new[] {"TenantId,NormalizedName", "TenantId,NormalizedCode"}, ignoreOrder: true);
    }

    [Test]
    public void ItemUsesExistingCategoryColumnWithRestrictiveTenantForeignKey()
    {
        using ApplicationDbContext context = Context();
        IEntityType item = context.Model.FindEntityType(typeof(InventoryItem))!;
        IForeignKey categoryKey = item.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(InventoryCategory));
        categoryKey.Properties.Select(property => property.Name).ShouldBe(new[] {"Category", "TenantId"});
        categoryKey.PrincipalKey.Properties.Select(property => property.Name).ShouldBe(new[] {"Code", "TenantId"});
        categoryKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        item.FindProperty("Category")!.GetMaxLength().ShouldBe(40);
        item.FindPrimaryKey()!.Properties.Select(property => property.Name).ShouldBe(new[] {"Id"});
    }

    [Test]
    public void CategoryHasNoLedgerOrCostRelationship()
    {
        using ApplicationDbContext context = Context();
        foreach (Type type in new[] {typeof(StockMovement), typeof(StockIssueLine), typeof(InputApplicationLine), typeof(OperationalCostPosting), typeof(StockReceiptLine)})
        {
            context.Model.FindEntityType(type)!.GetForeignKeys().ShouldAllBe(key => key.PrincipalEntityType.ClrType != typeof(InventoryCategory));
        }
    }

    [Test]
    public void MigrationOnlyAddsCategoryInfrastructureAndPreservesExactLegacyCodes()
    {
        MigrationBuilder builder = new("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddInventoryCategoryAdministration).GetMethod("Up", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(new AddInventoryCategoryAdministration(), new object[] {builder});
        builder.Operations.All(operation => operation is CreateTableOperation or CreateIndexOperation or AddForeignKeyOperation or SqlOperation).ShouldBeTrue();
        builder.Operations.OfType<CreateTableOperation>().Single().Name.ShouldBe("InventoryCategories");
        string sql = builder.Operations.OfType<SqlOperation>().Single().Sql;
        sql.ShouldContain("INSERT INTO inventory.\"InventoryCategories\"");
        sql.ShouldContain("md5(\"TenantId\"::text || ':inventory-category:' || \"Code\")::uuid");
        foreach (string code in Enum.GetNames<InventoryItemCategory>()) sql.ShouldContain("'" + code + "'");
        sql.ShouldNotContain("UPDATE ");
        sql.ShouldNotContain("DELETE ");
        sql.ShouldNotContain("StockMovements");
    }

    [Test]
    public void MigrationDownRefusesLossOfConfigurableCategoryData()
    {
        MigrationBuilder builder = new("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddInventoryCategoryAdministration).GetMethod("Down", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(new AddInventoryCategoryAdministration(), new object[] {builder});
        builder.Operations.OfType<SqlOperation>().Single().Sql.ShouldContain("RAISE EXCEPTION");
        builder.Operations.ShouldAllBe(operation => operation is SqlOperation);
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql().Options);
}
