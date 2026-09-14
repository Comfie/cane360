using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddMillRecordsAndStatementReconciliation is explicitly approved and applied to Railway Development.")]
[Category("Phase7CPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlMillRecordsAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [Test]
    public async Task RecordedTicketUpdateIsRejected()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertRejectedAsync("23514", "Recorded weighbridge tickets are immutable",
            "UPDATE mill.\"WeighbridgeTickets\" SET \"NetTonnes\"=14 WHERE \"Id\"=@id", ("id", source.TicketId));
    }

    [Test]
    public async Task RecordedTicketDeleteIsRejected()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertRejectedAsync("23514", "Recorded weighbridge tickets are immutable",
            "DELETE FROM mill.\"WeighbridgeTickets\" WHERE \"Id\"=@id", ("id", source.TicketId));
    }

    [Test]
    public async Task RecordedStatementUpdateIsRejected()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertRejectedAsync("23514", "Recorded grower statements are immutable",
            "UPDATE mill.\"GrowerStatements\" SET \"TotalTonnes\"=14 WHERE \"Id\"=@id", ("id", source.StatementId));
    }

    [Test]
    public async Task RecordedStatementDeleteIsRejected()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertRejectedAsync("23514", "Recorded grower statements are immutable",
            "DELETE FROM mill.\"GrowerStatements\" WHERE \"Id\"=@id", ("id", source.StatementId));
    }

    [Test]
    public async Task AuthoritativeMatchMutationIsProtected()
    {
        Scenario source = await CreateScenarioAsync(withMatch: true);
        await AssertRejectedAsync("23514", "StatementTicketMatches records are append-only",
            "UPDATE mill.\"StatementTicketMatches\" SET \"MatchedTonnes\"=1 WHERE \"Id\"=@id", ("id", source.MatchId!.Value));
    }

    [Test]
    public async Task TenantFarmMillTicketForeignKeysAreEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertForeignKeyRejectedAsync("FK_WeighbridgeTickets_", InsertTicketSql, TicketParameters(source,
            Guid.NewGuid(), "AUTOTEST-P7C-CROSS-SCOPE", Guid.NewGuid()));
    }

    [Test]
    public async Task TenantFarmStatementForeignKeysAreEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertForeignKeyRejectedAsync("FK_GrowerStatements_", InsertDraftStatementSql, StatementParameters(source,
            Guid.NewGuid(), "AUTOTEST-P7C-CROSS-SCOPE", Guid.NewGuid()));
    }

    [Test]
    public async Task FieldCropCycleConsistencyIsEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertForeignKeyRejectedAsync("FK_WeighbridgeTickets_", InsertTicketSqlWithAssociation,
            TicketParameters(source, Guid.NewGuid(), "AUTOTEST-P7C-FIELD-CYCLE", source.FarmId)
                .Concat([("field", (object)Guid.NewGuid()), ("cycle", source.CropCycleId)]).ToArray());
    }

    [Test]
    public async Task UniqueMillTicketReferenceIsEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        await AssertRejectedAsync("23505", "An active ticket reference already exists for this mill",
            InsertTicketSql, TicketParameters(source, Guid.NewGuid(),
            source.TicketReference.ToLowerInvariant(), source.FarmId));
    }

    [Test]
    public async Task StatementTicketMatchUniquenessIsEnforced()
    {
        Scenario source = await CreateScenarioAsync(withMatch: true);
        await AssertRejectedAsync("23505", "The ticket is already active in this statement",
            InsertMatchSql, MatchParameters(source, Guid.NewGuid(),
            source.StatementId, source.TicketId, 1m));
    }

    [Test]
    public async Task MatchMillConsistencyIsEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        Guid millId = Guid.NewGuid(); Guid ticketId = Guid.NewGuid();
        await ExecuteAsync(InsertMillSql, ("id", millId), ("tenant", source.TenantId),
            ("farm", source.FarmId), ("code", $"P7C-{millId:N}"[..20].ToUpperInvariant()),
            ("user", source.UserId), ("now", DateTimeOffset.UtcNow));
        await ExecuteAsync(InsertTicketSql, TicketParameters(source with { MillId = millId },
            ticketId, $"AUTOTEST-P7C-{ticketId:N}", source.FarmId));
        await AssertRejectedAsync("23514", "Matches require recorded statement and ticket evidence for the same mill and scope",
            InsertMatchSql, MatchParameters(source, Guid.NewGuid(),
            source.StatementId, ticketId, 1m));
    }

    [Test]
    public async Task CrossTenantMatchIsRejected()
    {
        Scenario source = await CreateScenarioAsync();
        (string Name, object Value)[] values = MatchParameters(source, Guid.NewGuid(),
            source.StatementId, source.TicketId, 1m);
        values = values.Select(x => x.Name == "tenant" ? (x.Name, (object)Guid.NewGuid()) : x).ToArray();
        await AssertForeignKeyRejectedAsync("FK_StatementTicketMatches_", InsertMatchSql, values);
    }

    [Test]
    public async Task MatchTonnageCapacityGuardIsEnforced()
    {
        Scenario source = await CreateScenarioAsync(withMatch: true);
        Scenario second = await CreateAdditionalStatementAsync(source);
        await AssertRejectedAsync("23514", "Active matched tonnes exceed the ticket net-tonnage capacity",
            InsertMatchSql, MatchParameters(source, Guid.NewGuid(),
            second.StatementId, source.TicketId, 1m));
    }

    [Test]
    public async Task TicketIdempotencyDuplicateConcurrencyReportsFinalState()
    {
        Scenario source = await CreateScenarioAsync();
        string reference = $"AUTOTEST-P7C-CONCURRENT-{Guid.NewGuid():N}";
        string idempotencyKey = $"AUTOTEST-P7C-CONCURRENT-{Guid.NewGuid():N}";
        Guid firstId = Guid.NewGuid(); Guid secondId = Guid.NewGuid();
        (string Name, object Value)[] first = TicketParameters(source, firstId, reference,
            source.FarmId).Select(x => x.Name == "key" ? (x.Name, (object)idempotencyKey) : x).ToArray();
        (string Name, object Value)[] second = TicketParameters(source, secondId, reference,
            source.FarmId).Select(x => x.Name == "key" ? (x.Name, (object)idempotencyKey) : x).ToArray();
        Exception?[] results = await ConcurrentAsync(InsertTicketSql,
            first, second);
        int rowCount = await CountAsync("SELECT count(*) FROM mill.\"WeighbridgeTickets\" WHERE \"TenantId\"=@tenant AND \"FarmId\"=@farm AND \"MillId\"=@mill AND \"TicketReference\"=@reference AND \"RecordingIdempotencyKey\"=@key AND \"Status\"='Recorded'",
            ("tenant", source.TenantId), ("farm", source.FarmId), ("mill", source.MillId),
            ("reference", reference.ToUpperInvariant()), ("key", idempotencyKey));
        results.Count(x => x is null).ShouldBe(1);
        PostgresException rejected = RequirePostgresRejection(results);
        rejected.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        rowCount.ShouldBe(1);
        TestContext.Progress.WriteLine($"SQLSTATE={rejected.SqlState}; row count={rowCount}; authoritative status=Recorded");
    }

    [Test]
    public async Task ConcurrentTicketMatchingReportsFinalState()
    {
        Scenario source = await CreateScenarioAsync();
        Scenario second = await CreateAdditionalStatementAsync(source);
        Exception?[] results = await ConcurrentAsync(InsertMatchSql,
            MatchParameters(source, Guid.NewGuid(), source.StatementId, source.TicketId, 15m),
            MatchParameters(source, Guid.NewGuid(), second.StatementId, source.TicketId, 15m));
        int activeCount = await CountAsync(ActiveMatchCountSql, ("tenant", source.TenantId),
            ("farm", source.FarmId), ("ticket", source.TicketId));
        decimal matched = Convert.ToDecimal(await ScalarAsync(ActiveMatchTonnesSql,
            ("tenant", source.TenantId), ("farm", source.FarmId), ("ticket", source.TicketId)));
        results.Count(x => x is null).ShouldBe(1);
        PostgresException rejected = RequirePostgresRejection(results);
        rejected.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        rejected.MessageText.ShouldContain("Active matched tonnes exceed the ticket net-tonnage capacity");
        activeCount.ShouldBe(1);
        matched.ShouldBe(15m);
        TestContext.Progress.WriteLine($"SQLSTATE={rejected.SqlState}; row count={activeCount}; active match count={activeCount}; matched tonnes={matched}; authoritative status=Recorded");
    }

    [Test]
    public async Task CorrectionLineageIntegrityIsEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        Guid draftId = Guid.NewGuid();
        await ExecuteAsync(InsertDraftTicketSql, TicketParameters(source, draftId,
            $"AUTOTEST-P7C-DRAFT-{draftId:N}", source.FarmId));
        await AssertRejectedAsync("23514", "Ticket correction lineage must target a recorded ticket for the same mill and scope",
            InsertCorrectionTicketSql,
            TicketParameters(source, Guid.NewGuid(), $"AUTOTEST-P7C-CORRECTION-{Guid.NewGuid():N}", source.FarmId)
                .Concat([("corrects", (object)draftId)]).ToArray());
    }

    [Test]
    public async Task StatementEvidenceIntegrityIsEnforced()
    {
        Scenario source = await CreateScenarioAsync();
        Guid statementId = Guid.NewGuid(); string reference = $"AUTOTEST-P7C-NO-EVIDENCE-{statementId:N}";
        await ExecuteAsync(InsertDraftStatementSql, StatementParameters(source, statementId,
            reference, source.FarmId));
        await AssertRejectedAsync("23514", "Original statement evidence is required before recording",
            "UPDATE mill.\"GrowerStatements\" SET \"Status\"='Recorded',\"RecordedByUserId\"=@user,\"RecordedAt\"=@now,\"RecordingIdempotencyKey\"=@key,\"Version\"=1 WHERE \"Id\"=@id",
            ("user", source.UserId), ("now", DateTimeOffset.UtcNow),
            ("key", $"AUTOTEST-P7C-{Guid.NewGuid():N}"), ("id", statementId));
    }

    [Test]
    public async Task MillAuditLinkIsAppendOnly()
    {
        Scenario source = await CreateScenarioAsync();
        Guid auditId = Guid.NewGuid(); Guid linkId = Guid.NewGuid();
        await ExecuteAsync(InsertAuditSql, ("id", auditId), ("tenant", source.TenantId),
            ("farm", source.FarmId), ("user", source.UserId), ("now", DateTimeOffset.UtcNow),
            ("subject", source.MillId));
        await ExecuteAsync(InsertAuditLinkSql, ("id", linkId), ("audit", auditId),
            ("tenant", source.TenantId), ("farm", source.FarmId), ("mill", source.MillId));
        await AssertRejectedAsync("23514", "MillRecordAuditEventLinks records are append-only",
            "DELETE FROM mill.\"MillRecordAuditEventLinks\" WHERE \"Id\"=@id", ("id", linkId));
    }

    private async Task<Scenario> CreateScenarioAsync(bool withMatch = false)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string label = $"AUTOTEST-P7C-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{suffix}";
        string userId = $"p7c-grower-{suffix}";
        Tenant tenant = Tenant.CreateForGrower(userId, label, null);
        CropVariety variety = tenant.AddCropVariety($"V{suffix}"[..20], "Synthetic cane");
        Farm farm = tenant.CreateFarm($"F{suffix}"[..20], label, "Synthetic address",
            "Railway Development", "Synthetic", 10m, "Synthetic");
        Field field = farm.AddField("P7C", "Synthetic field", 10m, null,
            ReportingAreaSource.Declared, "Synthetic", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2041, 1, 1), new DateOnly(2041, 11, 1),
            new DateOnly(2042, 1, 31), 100m, DateTimeOffset.UtcNow, userId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, userId);
        await using (ApplicationDbContext context = Context())
        {
            context.Users.Add(User(userId)); context.Tenants.Add(tenant); await context.SaveChangesAsync();
        }
        Mill mill = Mill.Create(tenant.Id, farm.Id, $"M{suffix}"[..20], label, null,
            userId, DateTimeOffset.UtcNow);
        WeighbridgeTicket ticket = WeighbridgeTicket.CreateDraft(tenant.Id, farm.Id, mill.Id,
            $"AUTOTEST-P7C-T-{suffix}", new DateOnly(2041, 11, 5), 20m, 5m, 15m,
            field.Id, cycle.Id, label, null, userId, DateTimeOffset.UtcNow);
        ticket.Record(userId, DateTimeOffset.UtcNow, $"AUTOTEST-P7C-TR-{suffix}", ticket.Version);
        GrowerStatement statement = GrowerStatement.CreateDraft(tenant.Id, farm.Id, mill.Id,
            $"AUTOTEST-P7C-S-{suffix}", new DateOnly(2041, 11, 1), new DateOnly(2041, 11, 30),
            15m, 100m, null, userId, DateTimeOffset.UtcNow);
        await using (ApplicationDbContext context = Context())
        {
            context.Mills.Add(mill); context.WeighbridgeTickets.Add(ticket);
            context.GrowerStatements.Add(statement); await context.SaveChangesAsync();
            EvidenceDocument evidence = EvidenceDocument.ForStatement(tenant.Id, farm.Id,
                statement.Id, "statement.pdf", "application/pdf", 1,
                Guid.NewGuid().ToString("N"), userId, DateTimeOffset.UtcNow);
            context.EvidenceDocuments.Add(evidence); await context.SaveChangesAsync();
            statement.Record(userId, DateTimeOffset.UtcNow, $"AUTOTEST-P7C-SR-{suffix}",
                statement.Version, true); await context.SaveChangesAsync();
        }
        Guid? matchId = null;
        if (withMatch)
        {
            StatementTicketMatch match = StatementTicketMatch.Add(tenant.Id, farm.Id,
                statement.Id, ticket.Id, 15m, null, true, null, userId,
                DateTimeOffset.UtcNow, $"AUTOTEST-P7C-M-{suffix}");
            await using ApplicationDbContext context = Context();
            context.StatementTicketMatches.Add(match); await context.SaveChangesAsync(); matchId = match.Id;
        }
        return new(tenant.Id, farm.Id, field.Id, cycle.Id, mill.Id, ticket.Id,
            statement.Id, matchId, userId, ticket.TicketReference);
    }

    private async Task<Scenario> CreateAdditionalStatementAsync(Scenario source)
    {
        Guid id = Guid.NewGuid(); string reference = $"AUTOTEST-P7C-S-{id:N}";
        await ExecuteAsync(InsertDraftStatementSql, StatementParameters(source, id, reference, source.FarmId));
        await ExecuteAsync("INSERT INTO mill.\"EvidenceDocuments\" (\"Id\",\"TenantId\",\"FarmId\",\"GrowerStatementId\",\"OriginalFileName\",\"ContentType\",\"SizeBytes\",\"StorageKey\",\"UploadedByUserId\",\"UploadedAt\") VALUES (@evidence,@tenant,@farm,@statement,'statement.pdf','application/pdf',1,@storage,@user,@now)",
            ("evidence", Guid.NewGuid()), ("tenant", source.TenantId), ("farm", source.FarmId),
            ("statement", id), ("storage", Guid.NewGuid().ToString("N")),
            ("user", source.UserId), ("now", DateTimeOffset.UtcNow));
        await ExecuteAsync("UPDATE mill.\"GrowerStatements\" SET \"Status\"='Recorded',\"RecordedByUserId\"=@user,\"RecordedAt\"=@now,\"RecordingIdempotencyKey\"=@key,\"Version\"=1 WHERE \"Id\"=@id",
            ("user", source.UserId), ("now", DateTimeOffset.UtcNow),
            ("key", $"AUTOTEST-P7C-{Guid.NewGuid():N}"), ("id", id));
        return source with { StatementId = id };
    }

    private async Task<Exception?[]> ConcurrentAsync(string sql,
        params (string Name, object Value)[][] parameterSets)
    {
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        return await Task.WhenAll(parameterSets.Select(async parameters =>
        {
            await using NpgsqlConnection connection = new(_connectionString); await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
            if (Interlocked.Increment(ref started) == parameterSets.Length) ready.SetResult();
            await ready.Task;
            try { await ExecuteAsync(connection, transaction, sql, parameters); await transaction.CommitAsync(); return null; }
            catch (Exception exception) { await transaction.RollbackAsync(); return exception; }
        }));
    }

    private async Task AssertRejectedAsync(string sqlState, string message,
        string sql, params (string Name, object Value)[] parameters)
    {
        PostgresException exception = await Should.ThrowAsync<PostgresException>(
            () => ExecuteAsync(sql, parameters));
        exception.SqlState.ShouldBe(sqlState);
        exception.MessageText.ShouldContain(message);
        TestContext.Progress.WriteLine($"SQLSTATE={exception.SqlState}; invariant={message}");
    }

    private async Task AssertForeignKeyRejectedAsync(string constraintPrefix,
        string sql, params (string Name, object Value)[] parameters)
    {
        PostgresException exception = await Should.ThrowAsync<PostgresException>(
            () => ExecuteAsync(sql, parameters));
        exception.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
        exception.ConstraintName.ShouldNotBeNull();
        exception.ConstraintName.ShouldStartWith(constraintPrefix);
        TestContext.Progress.WriteLine(
            $"SQLSTATE={exception.SqlState}; constraint={exception.ConstraintName}");
    }

    private static PostgresException RequirePostgresRejection(Exception?[] results) =>
        results.Single(x => x is not null) as PostgresException ??
        throw new AssertionException("The rejected concurrent operation must expose a PostgreSQL invariant.");
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    { await using NpgsqlConnection connection = new(_connectionString); await connection.OpenAsync(); await ExecuteAsync(connection, null, sql, parameters); }
    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string sql, params (string Name, object Value)[] parameters)
    { await using NpgsqlCommand command = new(sql, connection, transaction); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
    private async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    { await using NpgsqlConnection connection = new(_connectionString); await connection.OpenAsync(); await using NpgsqlCommand command = new(sql, connection); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
    private async Task<int> CountAsync(string sql, params (string Name, object Value)[] parameters) => Convert.ToInt32(await ScalarAsync(sql, parameters));
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private static string LoadConfiguredConnectionString() { string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }

    private static (string Name, object Value)[] TicketParameters(Scenario source, Guid id,
        string reference, Guid farmId) => [("id", id), ("tenant", source.TenantId),
        ("farm", farmId), ("mill", source.MillId), ("reference", reference.ToUpperInvariant()),
        ("user", source.UserId), ("now", DateTimeOffset.UtcNow),
        ("key", $"AUTOTEST-P7C-{Guid.NewGuid():N}")];
    private static (string Name, object Value)[] StatementParameters(Scenario source, Guid id,
        string reference, Guid farmId) => [("id", id), ("tenant", source.TenantId),
        ("farm", farmId), ("mill", source.MillId), ("reference", reference.ToUpperInvariant()),
        ("user", source.UserId), ("now", DateTimeOffset.UtcNow),
        ("key", $"AUTOTEST-P7C-{Guid.NewGuid():N}")];
    private static (string Name, object Value)[] MatchParameters(Scenario source, Guid id,
        Guid statementId, Guid ticketId, decimal tonnes) => [("id", id),
        ("tenant", source.TenantId), ("farm", source.FarmId), ("statement", statementId),
        ("ticket", ticketId), ("tonnes", tonnes), ("user", source.UserId),
        ("now", DateTimeOffset.UtcNow), ("key", $"AUTOTEST-P7C-{Guid.NewGuid():N}")];

    private const string InsertMillSql = "INSERT INTO mill.\"Mills\" (\"Id\",\"TenantId\",\"FarmId\",\"Code\",\"Name\",\"Active\",\"CreatedByUserId\",\"CreatedAt\",\"Version\") VALUES (@id,@tenant,@farm,@code,'AUTOTEST-P7C mill',true,@user,@now,0)";
    private const string InsertTicketSql = "INSERT INTO mill.\"WeighbridgeTickets\" (\"Id\",\"TenantId\",\"FarmId\",\"MillId\",\"TicketReference\",\"TicketDate\",\"GrossTonnes\",\"TareTonnes\",\"NetTonnes\",\"Status\",\"CreatedByUserId\",\"CreatedAt\",\"RecordedByUserId\",\"RecordedAt\",\"RecordingIdempotencyKey\",\"Version\") VALUES (@id,@tenant,@farm,@mill,@reference,'2041-11-05',20,5,15,'Recorded',@user,@now,@user,@now,@key,1)";
    private const string InsertDraftTicketSql = "INSERT INTO mill.\"WeighbridgeTickets\" (\"Id\",\"TenantId\",\"FarmId\",\"MillId\",\"TicketReference\",\"TicketDate\",\"GrossTonnes\",\"TareTonnes\",\"NetTonnes\",\"Status\",\"CreatedByUserId\",\"CreatedAt\",\"Version\") VALUES (@id,@tenant,@farm,@mill,@reference,'2041-11-05',20,5,15,'Draft',@user,@now,0)";
    private const string InsertCorrectionTicketSql = "INSERT INTO mill.\"WeighbridgeTickets\" (\"Id\",\"TenantId\",\"FarmId\",\"MillId\",\"TicketReference\",\"TicketDate\",\"GrossTonnes\",\"TareTonnes\",\"NetTonnes\",\"Status\",\"CreatedByUserId\",\"CreatedAt\",\"CorrectsTicketId\",\"CorrectionReason\",\"Version\") VALUES (@id,@tenant,@farm,@mill,@reference,'2041-11-05',20,5,15,'Draft',@user,@now,@corrects,'AUTOTEST-P7C correction',0)";
    private const string InsertTicketSqlWithAssociation = "INSERT INTO mill.\"WeighbridgeTickets\" (\"Id\",\"TenantId\",\"FarmId\",\"MillId\",\"TicketReference\",\"TicketDate\",\"GrossTonnes\",\"TareTonnes\",\"NetTonnes\",\"FieldId\",\"CropCycleId\",\"Status\",\"CreatedByUserId\",\"CreatedAt\",\"RecordedByUserId\",\"RecordedAt\",\"RecordingIdempotencyKey\",\"Version\") VALUES (@id,@tenant,@farm,@mill,@reference,'2041-11-05',20,5,15,@field,@cycle,'Recorded',@user,@now,@user,@now,@key,1)";
    private const string InsertDraftStatementSql = "INSERT INTO mill.\"GrowerStatements\" (\"Id\",\"TenantId\",\"FarmId\",\"MillId\",\"StatementReference\",\"PeriodStart\",\"PeriodEnd\",\"TotalTonnes\",\"TotalAmountUsd\",\"Status\",\"CreatedByUserId\",\"CreatedAt\",\"Version\") VALUES (@id,@tenant,@farm,@mill,@reference,'2041-11-01','2041-11-30',15,100,'Draft',@user,@now,0)";
    private const string InsertMatchSql = "INSERT INTO mill.\"StatementTicketMatches\" (\"Id\",\"TenantId\",\"FarmId\",\"GrowerStatementId\",\"WeighbridgeTicketId\",\"MatchedTonnes\",\"CompletesMatching\",\"CreatedByUserId\",\"CreatedAt\",\"Action\",\"IdempotencyKey\") VALUES (@id,@tenant,@farm,@statement,@ticket,@tonnes,true,@user,@now,'Added',@key)";
    private const string InsertAuditSql = "INSERT INTO audit.\"AuditEvents\" (\"Id\",\"TenantId\",\"FarmId\",\"SubjectType\",\"SubjectId\",\"Action\",\"AuthenticatedUserId\",\"SecurityRole\",\"OccurredAt\",\"CorrelationId\",\"SafeSummary\") VALUES (@id,@tenant,@farm,'MillRecords',@subject,'AUTOTEST-P7C',@user,'Grower',@now,CAST(@id AS text),'AUTOTEST-P7C audit')";
    private const string InsertAuditLinkSql = "INSERT INTO mill.\"MillRecordAuditEventLinks\" (\"Id\",\"AuditEventId\",\"TenantId\",\"FarmId\",\"MillId\") VALUES (@id,@audit,@tenant,@farm,@mill)";
    private const string ActiveMatchCountSql = "SELECT count(*) FROM mill.\"StatementTicketMatches\" active WHERE active.\"TenantId\"=@tenant AND active.\"FarmId\"=@farm AND active.\"WeighbridgeTicketId\"=@ticket AND active.\"Action\"='Added' AND NOT EXISTS (SELECT 1 FROM mill.\"StatementTicketMatches\" reversal WHERE reversal.\"ReversesMatchId\"=active.\"Id\")";
    private const string ActiveMatchTonnesSql = "SELECT COALESCE(sum(active.\"MatchedTonnes\"),0) FROM mill.\"StatementTicketMatches\" active WHERE active.\"TenantId\"=@tenant AND active.\"FarmId\"=@farm AND active.\"WeighbridgeTicketId\"=@ticket AND active.\"Action\"='Added' AND NOT EXISTS (SELECT 1 FROM mill.\"StatementTicketMatches\" reversal WHERE reversal.\"ReversesMatchId\"=active.\"Id\")";

    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid FieldId, Guid CropCycleId,
        Guid MillId, Guid TicketId, Guid StatementId, Guid? MatchId, string UserId,
        string TicketReference);
}
