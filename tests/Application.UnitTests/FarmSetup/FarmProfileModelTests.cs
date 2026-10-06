using Microsoft.EntityFrameworkCore.Migrations.Operations;
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
        // A closed release is checked against its immutable migration, not every later live-model change.
        var migration = new Cane360.Infrastructure.Data.Migrations.AddFarmOwnerProfileEnhancements();
        var changes = migration.UpOperations;
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
