using Cane360.Application.Activities;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class CreateResponseContractTests
{
    [TestCase(typeof(FinanceController), nameof(FinanceController.CreateTransaction))]
    [TestCase(typeof(FinanceController), nameof(FinanceController.CreateBudget))]
    [TestCase(typeof(MillRecordsController), nameof(MillRecordsController.CreateTicket))]
    [TestCase(typeof(MillRecordsController), nameof(MillRecordsController.CreateStatement))]
    [TestCase(typeof(WorkersController), nameof(WorkersController.Create))]
    public void CreatedActionsDocumentTheirCreatedResponse(Type controller, string actionName)
    {
        var action = controller.GetMethod(actionName)!;
        var resultType = action.ReturnType.GenericTypeArguments[0].GenericTypeArguments[0];
        var responses = action.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .Cast<ProducesResponseTypeAttribute>();

        responses.Any(response => response.StatusCode == StatusCodes.Status201Created &&
            response.Type == resultType).ShouldBeTrue();
    }

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
