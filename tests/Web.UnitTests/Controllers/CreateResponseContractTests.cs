using Cane360.Application.Activities;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class CreateResponseContractTests
{
    [Test]
    public async Task ActivityTypeCreateReturnsDtoWithoutListLocation()
    {
        var sender = new Mock<ISender>();
        var expected = new ActivityTypeDto(Guid.NewGuid(), "WEED", "Weeding", true, false,
            "Hectares", "Active", 1);
        sender.Setup(value => value.Send(It.IsAny<CreateActivityTypeCommand>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new ActivityTypesController(sender.Object).Create(
            new CreateActivityTypeRequest("WEED", "Weeding", true, false, "Hectares"),
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task PersonnelCreateReturnsRegisterWithoutListLocation()
    {
        var sender = new Mock<ISender>();
        var expected = new PersonnelRegisterDto(false, []);
        sender.Setup(value => value.Send(It.IsAny<CreatePersonCommand>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new FarmPersonnelController(sender.Object).Create(
            new CreatePersonRequest("Supervisor", null, new DateOnly(2026, 9, 17),
                ["Supervisor"], false), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }
}
