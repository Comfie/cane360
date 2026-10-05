using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.FarmSetup;

public sealed class FarmProfileModelTests
{
    [Test]
    public void ReleaseMigrationIsAdditiveAndDoesNotSeedData()
    {
        var migration = new Cane360.Infrastructure.Data.Migrations.AddFarmOwnerProfileEnhancements();
        foreach (var operation in migration.UpOperations)
        {
            operation.IsDestructiveChange.ShouldBeFalse();
            (operation is AddColumnOperation or CreateTableOperation or CreateIndexOperation or
                AddCheckConstraintOperation or AddForeignKeyOperation).ShouldBeTrue();
        }
    }

    [Test]
    public void ModelChangesStayWithinFarmProfileRelease()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot!;
        var initializer = context.GetService<IModelRuntimeInitializer>();
        var before = initializer.Initialize(snapshot.Model, designTime: true);
        var after = context.GetService<IDesignTimeModel>().Model;
        var changes = context.GetService<IMigrationsModelDiffer>().GetDifferences(
            before.GetRelationalModel(), after.GetRelationalModel());
        foreach (var change in changes)
        {
            TestContext.Out.WriteLine($"{change.GetType().Name}: {change.GetType().GetProperty("Table")?.GetValue(change)} {change.GetType().GetProperty("Name")?.GetValue(change)}");
            string? table = change.GetType().GetProperty("Table")?.GetValue(change) as string;
            if (change is CreateTableOperation create) table = create.Name;
            table.ShouldBeOneOf("GrowerProfiles", "Farms", "FarmModels");
            change.IsDestructiveChange.ShouldBeFalse();
        }
    }
}
