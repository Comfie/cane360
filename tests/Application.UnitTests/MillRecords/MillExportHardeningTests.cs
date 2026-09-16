using Cane360.Application.Common.Interfaces;
using Cane360.Application.MillRecords;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.MillRecords;

public sealed class MillExportHardeningTests
{
    [Test]
    public async Task ExportContextAndAuditBindAuthenticatedFarmAndExactFilters()
    {
        const string userId = "AUTOTEST-P8A-grower";
        Tenant tenant = Tenant.CreateForGrower(userId, "AUTOTEST-P8A", null);
        Farm farm = tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic",
            "Synthetic", "Other", 10m, "Synthetic");
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(x => x.GetTenantReferenceContextForUserAsync(userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var records = new Mock<IMillRecordsRepository>();
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        user.SetupGet(x => x.CorrelationId).Returns("AUTOTEST-P8A-reference");
        var service = new MillRecordsService(farms.Object, records.Object,
            Mock.Of<IEvidenceDocumentStorage>(), user.Object, TimeProvider.System);

        var context = await service.RecordExportAsync("WeighbridgeRegister", "?from=2041-01-01", CancellationToken.None);

        context.Farm.ShouldBe(farm.Name);
        context.Filters.ShouldBe("?from=2041-01-01");
        records.Verify(x => x.Add(It.Is<MillRecordExport>(export => export.TenantId == tenant.Id &&
            export.FarmId == farm.Id && export.CreatedByUserId == userId &&
            export.Filters == context.Filters && export.CreatedAt == context.GeneratedAt)), Times.Once);
        records.Verify(x => x.Add(It.Is<AuditEvent>(audit => audit.TenantId == tenant.Id &&
            audit.FarmId == farm.Id && audit.AuthenticatedUserId == userId &&
            audit.CorrelationId == "AUTOTEST-P8A-reference")), Times.Once);
        records.Verify(x => x.Add(It.IsAny<MillRecordAuditEventLink>()), Times.Once);
        records.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnauthenticatedExportDoesNotCreateAnyAuditOrExportFact()
    {
        var records = new Mock<IMillRecordsRepository>(MockBehavior.Strict);
        var service = new MillRecordsService(Mock.Of<IFarmSetupRepository>(), records.Object,
            Mock.Of<IEvidenceDocumentStorage>(), Mock.Of<IUser>(), TimeProvider.System);
        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ForbiddenAccessException>(() =>
            service.RecordExportAsync("WeighbridgeRegister", "", CancellationToken.None));
        records.VerifyNoOtherCalls();
    }
}
