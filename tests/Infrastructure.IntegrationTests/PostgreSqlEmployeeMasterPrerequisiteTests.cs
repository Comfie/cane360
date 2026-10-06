using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Creates only a labelled synthetic legacy employee graph before CR-01.3 migration; retained for acceptance.")]
[Category("CR013Prerequisite")]
[NonParallelizable]
public sealed class PostgreSqlEmployeeMasterPrerequisiteTests
{
    [Test]
    public async Task CreateSyntheticLegacyWorkerAndDownstreamLinksUsingExistingColumnsOnly()
    {
        string label = Environment.GetEnvironmentVariable("CANE360_CR013_LEGACY_LABEL")
            ?? throw new InvalidOperationException("A unique synthetic label is required.");
        label.ShouldStartWith("AUTOTEST-CR013-LEGACY-");
        await using ApplicationDbContext context = EmployeeMasterAcceptanceFixture.Context();
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        await using var transaction = await context.Database.BeginTransactionAsync();
        EmployeeMasterAcceptanceFixture fixture = new(label);
        await fixture.Seed(context, legacyColumnsOnly: true);
        await transaction.CommitAsync();
    }
}
