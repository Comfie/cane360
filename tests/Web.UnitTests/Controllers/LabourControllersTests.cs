using Cane360.Application.Labour;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class LabourControllersTests
{
    [TestCase(typeof(WorkersController))]
    [TestCase(typeof(WorkerRatesController))]
    [TestCase(typeof(AttendanceController))]
    [TestCase(typeof(WorkRecordsController))]
    public void LabourControllersRequireAuthentication(Type controllerType)
    {
        controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
    }

    [Test]
    public async Task AttendanceMapsWorkerDateStatusFieldAndVersion()
    {
        var sender = new Mock<ISender>();
        var workerId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 18);
        var expected = new AttendanceRegisterDto(date, [], []);
        sender.Setup(service => service.Send(
            It.Is<RecordAttendanceCommand>(command =>
                command.WorkDate == date &&
                command.LateEntryReason == "Source sheet received late" &&
                command.Entries.Count == 1 &&
                command.Entries[0].WorkerId == workerId &&
                command.Entries[0].Status == "Present" &&
                command.Entries[0].FieldId == fieldId &&
                command.Entries[0].ExpectedVersion == 3),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new AttendanceController(sender.Object);

        var result = await controller.Record(new RecordAttendanceRequest(
            "2026-08-18", "Source sheet received late", [new AttendanceEntryRequest(workerId, "Present", fieldId, 3)]),
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task AttendanceQueryMapsInvariantDateWithoutTimezoneConversion()
    {
        var sender = new Mock<ISender>();
        var date = new DateOnly(2026, 8, 18);
        var expected = new AttendanceRegisterDto(date, [], []);
        sender.Setup(service => service.Send(
            It.Is<GetAttendanceRegisterQuery>(query => query.WorkDate == date),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new AttendanceController(sender.Object);

        var result = await controller.Get("2026-08-18", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task AttendanceQueryRejectsTimestampInsteadOfShiftingItsDate()
    {
        var sender = new Mock<ISender>();
        var controller = new AttendanceController(sender.Object);

        var result = await controller.Get("2026-08-18T00:00:00.000Z", CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        sender.Verify(service => service.Send(It.IsAny<GetAttendanceRegisterQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task WorkVerificationAndConfirmationAreSeparateRoutes()
    {
        var sender = new Mock<ISender>();
        var recordId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var expected = WorkRecord(recordId);
        sender.Setup(service => service.Send(
            It.Is<VerifyWorkRecordCommand>(command => command.WorkRecordId == recordId &&
                command.SupervisorPersonId == supervisorId && command.ExpectedVersion == 2),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        sender.Setup(service => service.Send(
            It.Is<ConfirmWorkRecordCommand>(command => command.WorkRecordId == recordId && command.ExpectedVersion == 3),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new WorkRecordsController(sender.Object);

        (await controller.Verify(recordId, new VerifyWorkRecordRequest(supervisorId, 2), CancellationToken.None))
            .Result.ShouldBeOfType<OkObjectResult>();
        (await controller.Confirm(recordId, new ConfirmWorkRecordRequest(3), CancellationToken.None))
            .Result.ShouldBeOfType<OkObjectResult>();

        sender.Verify(service => service.Send(It.IsAny<VerifyWorkRecordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(service => service.Send(It.IsAny<ConfirmWorkRecordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static WorkRecordDto WorkRecord(Guid id) => new(
        id, Guid.NewGuid(), "Worker", Guid.NewGuid(), Guid.NewGuid(), "Field", new DateOnly(2026, 8, 18),
        "Daily", 12m, null, null, "Draft", [Guid.NewGuid()], ["Weeding"], [], null,
        new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero), 0, null, 0);
}
