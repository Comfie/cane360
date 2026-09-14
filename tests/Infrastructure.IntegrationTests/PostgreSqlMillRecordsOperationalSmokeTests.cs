using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.MillRecords;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after the Phase 7C Railway migration and gated acceptance pass.")]
[Category("Phase7COperationalSmoke")]
[NonParallelizable]
public sealed class PostgreSqlMillRecordsOperationalSmokeTests
{
    private string _connectionString = string.Empty;
    private IEvidenceDocumentStorage _storage = null!;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET")
            .ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["EvidenceStorage:RootPath"] = Path.Combine(Path.GetTempPath(),
                    "cane360-p7c-private-evidence")
            }).Build();
        _storage = new FileSystemEvidenceDocumentStorage(configuration,
            new AcceptanceHostEnvironment());
    }

    [Test]
    public async Task GoldenPathReconcilesAuthoritativeMillEvidenceWithoutSideEffects()
    {
        SmokeScope scope = await SeedScopeAsync();
        long[] before = await SideEffectCountsAsync(scope.TenantId, scope.FarmId);

        MillDto mill = await InvokeAsync(scope.UserId, service => service.CreateMillAsync(
            new(" smoke ", "AUTOTEST-P7C operational mill", "Synthetic"), default));
        mill.Active.ShouldBeTrue();
        mill.Code.ShouldBe("SMOKE");

        await Should.ThrowAsync<ValidationException>(() => InvokeAsync(scope.UserId,
            service => service.CreateTicketAsync(new(mill.Id, "AUTOTEST-P7C-BAD-WEIGHT",
                new DateOnly(2041, 11, 5), 20m, 5m, 16m, null, null, null, null), default)));

        WeighbridgeTicketDto firstDraft = await InvokeAsync(scope.UserId,
            service => service.CreateTicketAsync(new(mill.Id, " autotest-p7c-smoke-001 ",
                new DateOnly(2041, 11, 5), 15m, 5m, 10m, null, null,
                "AUTOTEST-P7C source", null), default));
        firstDraft.FieldId.ShouldBeNull();
        firstDraft.CropCycleId.ShouldBeNull();
        WeighbridgeTicketDto firstRecorded = await InvokeAsync(scope.UserId,
            service => service.RecordTicketAsync(firstDraft.Id,
                new(firstDraft.Version, Key("record-ticket-1")), default));
        firstRecorded.Status.ShouldBe("Recorded");

        await Should.ThrowAsync<ConflictException>(() => InvokeAsync(scope.UserId,
            service => service.CreateTicketAsync(new(mill.Id, "AUTOTEST-P7C-SMOKE-001",
                new DateOnly(2041, 11, 5), 15m, 5m, 10m, null, null, null, null), default)));
        await Should.ThrowAsync<ValidationException>(() => InvokeAsync(scope.UserId,
            service => service.UpdateTicketAsync(firstRecorded.Id, new(mill.Id,
                firstRecorded.TicketReference, new DateOnly(2041, 11, 5), 14m, 5m, 9m,
                null, null, null, null, firstRecorded.Version), default)));

        WeighbridgeTicketDto corrected = await InvokeAsync(scope.UserId,
            service => service.CorrectTicketAsync(firstRecorded.Id, new(
                "AUTOTEST-P7C corrected source weight", Key("correct-ticket"),
                new(mill.Id, firstRecorded.TicketReference, new DateOnly(2041, 11, 5),
                    15m, 5m, 10m, null, null, "AUTOTEST-P7C corrected source", null,
                    firstRecorded.Version)), default));
        corrected.CorrectsTicketId.ShouldBe(firstRecorded.Id);
        corrected.Status.ShouldBe("Recorded");
        WeighbridgeTicketDto preservedOriginal = await InvokeAsync(scope.UserId,
            service => service.GetTicketAsync(firstRecorded.Id, default));
        preservedOriginal.Status.ShouldBe("Recorded");
        preservedOriginal.CorrectedByTicketId.ShouldBe(corrected.Id);

        WeighbridgeTicketDto associatedDraft = await InvokeAsync(scope.UserId,
            service => service.CreateTicketAsync(new(mill.Id, "AUTOTEST-P7C-SMOKE-002",
                new DateOnly(2041, 11, 6), 20m, 5m, 15m, scope.FieldId,
                scope.CropCycleId, "AUTOTEST-P7C source", null), default));
        WeighbridgeTicketDto associated = await InvokeAsync(scope.UserId,
            service => service.RecordTicketAsync(associatedDraft.Id,
                new(associatedDraft.Version, Key("record-ticket-2")), default));
        associated.FieldId.ShouldBe(scope.FieldId);
        associated.CropCycleId.ShouldBe(scope.CropCycleId);
        associated.RecordedHarvestTonnes.ShouldBe(40m);
        (await HarvestTonnesAsync(scope.CropCycleId)).ShouldBe(40m);

        GrowerStatementDto statementDraft = await InvokeAsync(scope.UserId,
            service => service.CreateStatementAsync(new(mill.Id,
                "AUTOTEST-P7C-SMOKE-STATEMENT-001", new DateOnly(2041, 11, 1),
                new DateOnly(2041, 11, 30), 20m, 100m, "AUTOTEST-P7C statement"), default));
        await Should.ThrowAsync<ValidationException>(() => InvokeAsync(scope.UserId,
            service => service.RecordStatementAsync(statementDraft.Id,
                new(statementDraft.Version, Key("record-no-evidence")), default)));

        EvidenceDocumentDto evidence = await InvokeAsync(scope.UserId, async service =>
        {
            byte[] bytes = "AUTOTEST-P7C private statement evidence"u8.ToArray();
            await using var stream = new MemoryStream(bytes, false);
            return await service.UploadStatementEvidenceAsync(statementDraft.Id,
                new(stream, "AUTOTEST-P7C-statement.txt", "text/plain", bytes.LongLength), default);
        });
        GrowerStatementDto statement = await InvokeAsync(scope.UserId,
            service => service.RecordStatementAsync(statementDraft.Id,
                new(statementDraft.Version, Key("record-statement-1")), default));
        statement.TotalTonnes.ShouldBe(20m);
        statement.TotalAmountUsd.ShouldBe(100m);
        statement.Reconciliation.Status.ShouldBe("Unmatched");

        await InvokeAsync(scope.UserId, async service =>
        {
            EvidenceDownload download = await service.OpenEvidenceAsync(evidence.Id, default);
            await using Stream content = download.Content;
            using var reader = new StreamReader(content);
            (await reader.ReadToEndAsync()).ShouldBe("AUTOTEST-P7C private statement evidence");
            return true;
        });

        ReconciliationSummaryDto partial = await InvokeAsync(scope.UserId,
            service => service.AddMatchAsync(statement.Id, new(corrected.Id, 5m, null,
                false, "AUTOTEST-P7C partial source allocation", Key("match-1"),
                statement.Version, corrected.Version), default));
        partial.Status.ShouldBe("PartiallyMatched");
        partial.MatchedTicketTonnes.ShouldBe(5m);
        partial.TonnesVariance.ShouldBe(15m);
        partial.AmountStatus.ShouldBe("NotAvailable");
        partial.MatchedAmountUsd.ShouldBeNull();

        ReconciliationSummaryDto matched = await InvokeAsync(scope.UserId,
            service => service.AddMatchAsync(statement.Id, new(associated.Id, 15m, null,
                true, null, Key("match-2"), statement.Version, associated.Version), default));
        matched.Status.ShouldBe("Matched");
        matched.MatchedTicketTonnes.ShouldBe(20m);
        matched.TonnesVariance.ShouldBe(0m);
        matched.AmountStatus.ShouldBe("NotAvailable");
        matched.AmountVarianceUsd.ShouldBeNull();

        GrowerStatementDto secondDraft = await InvokeAsync(scope.UserId,
            service => service.CreateStatementAsync(new(mill.Id,
                "AUTOTEST-P7C-SMOKE-STATEMENT-002", new DateOnly(2041, 11, 1),
                new DateOnly(2041, 11, 30), 5m, 25m, null), default));
        await InvokeAsync(scope.UserId, async service =>
        {
            byte[] bytes = "AUTOTEST-P7C private second statement"u8.ToArray();
            await using var stream = new MemoryStream(bytes, false);
            return await service.UploadStatementEvidenceAsync(secondDraft.Id,
                new(stream, "AUTOTEST-P7C-statement-2.txt", "text/plain", bytes.LongLength), default);
        });
        GrowerStatementDto second = await InvokeAsync(scope.UserId,
            service => service.RecordStatementAsync(secondDraft.Id,
                new(secondDraft.Version, Key("record-statement-2")), default));
        await Should.ThrowAsync<ValidationException>(() => InvokeAsync(scope.UserId,
            service => service.AddMatchAsync(second.Id, new(corrected.Id, 6m, null,
                true, null, Key("over-capacity"), second.Version, corrected.Version), default)));
        ReconciliationSummaryDto secondMatched = await InvokeAsync(scope.UserId,
            service => service.AddMatchAsync(second.Id, new(corrected.Id, 5m, null,
                true, null, Key("cross-statement-match"), second.Version,
                corrected.Version), default));
        secondMatched.Status.ShouldBe("Matched");
        ReconciliationSummaryDto reuse = await InvokeAsync(scope.UserId,
            service => service.GetReconciliationAsync(statement.Id, default));
        reuse.HasCrossStatementTicketReuse.ShouldBeTrue();
        reuse.MatchedTicketTonnes.ShouldBe(20m);

        await AssertTenantIsolationAsync(scope, corrected.Id, statement.Id, evidence.Id,
            associated.Id);

        IReadOnlyList<WeighbridgeTicketDto> ticketRows = await InvokeAsync(scope.UserId,
            service => service.GetTicketsAsync(new(new DateOnly(2041, 11, 1),
                new DateOnly(2041, 11, 30), mill.Id, null, null, "Recorded", null,
                "AUTOTEST-P7C-SMOKE"), default));
        ticketRows.Where(x => x.IsCurrent).Sum(x => x.NetTonnes).ShouldBe(25m);
        IReadOnlyList<GrowerStatementDto> statementRows = await InvokeAsync(scope.UserId,
            service => service.GetStatementsAsync(new(new DateOnly(2041, 11, 1),
                new DateOnly(2041, 11, 30), mill.Id, null, "AUTOTEST-P7C-SMOKE"), default));
        statementRows.Where(x => x.IsCurrent).Sum(x => x.TotalTonnes).ShouldBe(25m);
        statementRows.Where(x => x.IsCurrent).Sum(x => x.Reconciliation.MatchedTicketTonnes)
            .ShouldBe(25m);
        await InvokeAsync(scope.UserId, async service =>
        {
            await service.RecordExportAsync("StatementReconciliation",
                "AUTOTEST-P7C scoped smoke filters", default);
            return true;
        });
        (await ExportCountAsync(scope.TenantId, scope.FarmId)).ShouldBe(1);

        long[] after = await SideEffectCountsAsync(scope.TenantId, scope.FarmId);
        after.ShouldBe(before);
        TestContext.Progress.WriteLine(
            "golden path=green; current ticket tonnes=25.000; statement tonnes=25.000; matched tonnes=25.000; tonnes variance=0.000; amount reconciliation=NotAvailable; cross-tenant disclosure=none; finance/payment side effects=0; export rows=1");
    }

    private async Task AssertTenantIsolationAsync(SmokeScope scope, Guid ticketId,
        Guid statementId, Guid evidenceId, Guid candidateTicketId)
    {
        await Should.ThrowAsync<NotFoundException>(() => InvokeAsync(scope.OtherUserId,
            service => service.GetTicketAsync(ticketId, default)));
        await Should.ThrowAsync<NotFoundException>(() => InvokeAsync(scope.OtherUserId,
            service => service.GetStatementAsync(statementId, default)));
        await Should.ThrowAsync<NotFoundException>(() => InvokeAsync(scope.OtherUserId,
            service => service.OpenEvidenceAsync(evidenceId, default)));
        await Should.ThrowAsync<NotFoundException>(() => InvokeAsync(scope.OtherUserId,
            service => service.AddMatchAsync(statementId, new(candidateTicketId, 1m, null,
                false, "AUTOTEST-P7C isolation", Key("cross-tenant"), 1, 1), default)));
    }

    private async Task<SmokeScope> SeedScopeAsync()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string userId = $"p7c-smoke-grower-{suffix}";
        string otherUserId = $"p7c-smoke-other-{suffix}";
        Tenant tenant = Tenant.CreateForGrower(userId, $"AUTOTEST-P7C-{suffix}", null);
        CropVariety variety = tenant.AddCropVariety($"V{suffix}"[..20], "Synthetic cane");
        Farm farm = tenant.CreateFarm($"F{suffix}"[..20], $"AUTOTEST-P7C-{suffix}",
            "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic");
        Field field = farm.AddField("P7C", "AUTOTEST-P7C field", 10m, null,
            ReportingAreaSource.Declared, "Synthetic", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2041, 1, 1), new DateOnly(2041, 10, 1),
            new DateOnly(2041, 12, 31), 40m, DateTimeOffset.UtcNow, userId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, userId);
        cycle.MarkReadyForHarvest(DateTimeOffset.UtcNow, userId);
        cycle.RecordHarvest(new DateOnly(2041, 11, 1), 40m, new DateOnly(2041, 11, 1),
            DateTimeOffset.UtcNow, userId);

        Tenant other = Tenant.CreateForGrower(otherUserId,
            $"AUTOTEST-P7C-OTHER-{suffix}", null);
        other.CreateFarm($"O{suffix}"[..20], $"AUTOTEST-P7C-OTHER-{suffix}",
            "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic");
        await using ApplicationDbContext context = Context();
        context.Users.AddRange(User(userId), User(otherUserId));
        context.Tenants.AddRange(tenant, other);
        await context.SaveChangesAsync();
        return new(tenant.Id, farm.Id, field.Id, cycle.Id, userId, otherUserId);
    }

    private async Task<T> InvokeAsync<T>(string userId,
        Func<IMillRecordsService, Task<T>> action)
    {
        await using ApplicationDbContext context = Context();
        var service = new MillRecordsService(new FarmSetupRepository(context),
            new MillRecordsRepository(context), _storage, new AcceptanceUser(userId),
            TimeProvider.System);
        return await action(service);
    }

    private async Task<long[]> SideEffectCountsAsync(Guid tenantId, Guid farmId)
    {
        const string sql = "SELECT (SELECT count(*) FROM finance.\"OperationalTransactions\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm), (SELECT count(*) FROM finance.\"OperationalCostPostings\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm), (SELECT count(*) FROM finance.\"Budgets\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm), (SELECT count(*) FROM payroll.\"PayrollPayments\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm)";
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("farm", farmId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        return [reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3)];
    }

    private async Task<decimal> HarvestTonnesAsync(Guid cropCycleId)
    {
        await using ApplicationDbContext context = Context();
        return await context.HarvestResults.Where(x => x.CropCycleId == cropCycleId)
            .Select(x => x.ActualTonnes).SingleAsync();
    }

    private async Task<int> ExportCountAsync(Guid tenantId, Guid farmId)
    {
        await using ApplicationDbContext context = Context();
        return await context.MillRecordExports.CountAsync(x => x.TenantId == tenantId &&
            x.FarmId == farmId);
    }

    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(_connectionString).Options);

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

    private static string Key(string subject) =>
        $"AUTOTEST-P7C-{subject}-{Guid.NewGuid():N}".ToUpperInvariant();

    private static string LoadConfiguredConnectionString()
    {
        string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db");
        if (!string.IsNullOrWhiteSpace(value)) return value;
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException(
            "The configured Railway development connection is unavailable.");
    }

    private sealed class AcceptanceUser(string id) : IUser
    {
        public string? Id => id;
        public List<string>? Roles => null;
        public string? CorrelationId => $"AUTOTEST-P7C-{Guid.NewGuid():N}";
    }

    private sealed class AcceptanceHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Cane360";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed record SmokeScope(Guid TenantId, Guid FarmId, Guid FieldId,
        Guid CropCycleId, string UserId, string OtherUserId);
}
