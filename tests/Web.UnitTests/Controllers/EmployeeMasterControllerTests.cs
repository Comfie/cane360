using Cane360.Application.Labour;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class EmployeeMasterControllerTests
{
    [Test]
    public async Task CreateMapsStructuredProfileAndKeepsExistingRouteIdentity()
    {
        var sender = new Mock<ISender>();
        Guid id = Guid.NewGuid();
        var expected = Details(id);
        var profile = new WorkerProfileInput(FirstName: "First", Surname: "Last");
        sender.Setup(service => service.Send(It.Is<CreateWorkerCommand>(command =>
            command.Profile == profile && command.ActiveFrom == new DateOnly(2026, 10, 5) && command.NationalId == "SYNTHETIC-12"),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new WorkersController(sender.Object).Create(new CreateWorkerRequest(null, "First Last", null,
            "Casual", "2026-10-05", "SYNTHETIC-12", profile), CancellationToken.None);
        var created = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBeSameAs(expected);
        created.RouteValues!["workerId"].ShouldBe(id);
    }

    [Test]
    public async Task ProfileUpdateMapsBothConcurrencyVersionsAndRouteId()
    {
        var sender = new Mock<ISender>();
        Guid id = Guid.NewGuid();
        var expected = Details(id);
        var profile = new WorkerProfileInput(PhotoReference: "asset:photo", NextOfKinName: "Kin");
        sender.Setup(service => service.Send(It.Is<UpdateWorkerProfileCommand>(command => command.WorkerId == id &&
            command.ExpectedVersion == 4 && command.ExpectedPersonVersion == 5 && command.Profile == profile &&
            command.Phone == "123" && command.EmploymentType == "Permanent"), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new WorkersController(sender.Object).UpdateProfile(id,
            new UpdateWorkerProfileRequest(4, 5, "Name", "123", "Permanent", profile), CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task NationalIdCorrectionAndRevealUseSeparateSecureCommands()
    {
        var sender = new Mock<ISender>();
        Guid id = Guid.NewGuid();
        sender.Setup(service => service.Send(It.Is<CorrectWorkerNationalIdCommand>(command => command.WorkerId == id &&
            command.ExpectedVersion == 2 && command.NationalId == "SYNTHETIC-12" && command.Reason == "Correction"),
            It.IsAny<CancellationToken>())).ReturnsAsync(Details(id));
        sender.Setup(service => service.Send(It.Is<RevealWorkerNationalIdCommand>(command => command.WorkerId == id &&
            command.Reason == "Check"), It.IsAny<CancellationToken>())).ReturnsAsync(new RevealedNationalIdDto(id, "SYNTHETIC12"));
        var controller = new WorkersController(sender.Object);
        (await controller.CorrectNationalId(id, new CorrectWorkerNationalIdRequest("SYNTHETIC-12", 2, "Correction"),
            CancellationToken.None)).Result.ShouldBeOfType<OkObjectResult>();
        (await controller.RevealNationalId(id, new RevealNationalIdRequest("Check"), CancellationToken.None))
            .Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<RevealedNationalIdDto>().WorkerId.ShouldBe(id);
    }

    private static WorkerDetailsDto Details(Guid id) => new(new WorkerListItemDto(id, Guid.NewGuid(), "Name",
        null, "Casual", new DateOnly(2026, 10, 5), null, "Active", "••••••12", 0), []);
}
