using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cane360.Web.UnitTests.Infrastructure;

public sealed class EmployeeMasterModelTests
{
    [Test]
    public void WorkerIdentityAndDownstreamCompositeForeignKeysStayUnchanged()
    {
        using var context = Context();
        IEntityType worker = context.Model.FindEntityType(typeof(WorkerProfile))!;
        worker.GetTableName().ShouldBe("WorkerProfiles");
        worker.GetSchema().ShouldBe("labour");
        worker.FindPrimaryKey()!.Properties.Select(p => p.Name).ShouldBe(new[] {"Id"});
        foreach (Type type in new[] {typeof(Attendance), typeof(WorkRecord), typeof(WorkerRate), typeof(WorkerAdvance), typeof(PayrollWorkerLine)})
        {
            var fk = context.Model.FindEntityType(type)!.GetForeignKeys().Single(key => key.PrincipalEntityType == worker);
            fk.Properties.Select(p => p.Name).ShouldBe(new[] {"WorkerProfileId", "TenantId", "FarmId"});
            fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        }
        context.Model.GetEntityTypes().Any(type => type.ClrType.Name == "Employee").ShouldBeFalse();
    }

    [Test]
    public void NewProfileColumnsAreNullableWithUniqueTenantNumberAndNoPhotoBinary()
    {
        using var context = Context();
        IEntityType worker = context.Model.FindEntityType(typeof(WorkerProfile))!;
        foreach (string name in new[] {"EmployeeNumber", "Title", "FirstName", "Surname", "Sex", "DateOfBirth", "Address",
            "PhotoReference", "NextOfKinName", "NextOfKinRelationship", "NextOfKinPhone", "NextOfKinAddress"})
            worker.FindProperty(name)!.IsNullable.ShouldBeTrue();
        worker.FindProperty("PhotoReference")!.ClrType.ShouldBe(typeof(string));
        var index = worker.GetIndexes().Single(item => item.GetDatabaseName() == "UX_WorkerProfiles_Tenant_EmployeeNumber");
        index.IsUnique.ShouldBeTrue();
        index.Properties.Select(p => p.Name).ShouldBe(new[] {"TenantId", "EmployeeNumber"});
        worker.FindProperty("Version")!.IsConcurrencyToken.ShouldBeTrue();
        worker.FindProperty("NationalIdCiphertext")!.IsNullable.ShouldBeFalse();
        worker.FindProperty("NationalId").ShouldBeNull();
    }

    [Test]
    public void EmployeeMigrationOnlyAddsNullableWorkerColumnsAndTenantNumberIndex()
    {
        var migration = new Cane360.Infrastructure.Data.Migrations.AddEmployeeMasterEnhancements();
        migration.UpOperations.Count.ShouldBe(13);
        foreach (var operation in migration.UpOperations)
        {
            operation.IsDestructiveChange.ShouldBeFalse();
            if (operation is Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation column)
            {
                column.Table.ShouldBe("WorkerProfiles"); column.Schema.ShouldBe("labour");
                column.IsNullable.ShouldBeTrue(); column.DefaultValue.ShouldBeNull(); column.DefaultValueSql.ShouldBeNull();
            }
            else
            {
                var index = operation.ShouldBeOfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>();
                index.Table.ShouldBe("WorkerProfiles"); index.Schema.ShouldBe("labour");
                index.IsUnique.ShouldBeTrue(); index.Columns.ShouldBe(new[] {"TenantId", "EmployeeNumber"});
            }
        }
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql("Host=localhost;Database=unused_model_only").Options);
}
