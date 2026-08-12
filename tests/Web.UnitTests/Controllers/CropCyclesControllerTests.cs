using Cane360.Application.CropCycles;
using Cane360.Web.Controllers;
using Cane360.Web.Models.CropCycles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class CropCyclesControllerTests
{
    [Test]
    public void ControllerRequiresAuthentication()
    {
        typeof(CropCyclesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .ShouldNotBeEmpty();
    }

    [Test]
    public async Task HarvestMapsRequestToApplicationCommand()
    {
        var sender = new Mock<ISender>();
        var fieldId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var expected = CreateDetails(fieldId, cycleId);
        sender.Setup(service => service.Send(
                It.Is<HarvestCropCycleCommand>(command =>
                    command.FieldId == fieldId &&
                    command.CropCycleId == cycleId &&
                    command.ExpectedVersion == 3 &&
                    command.HarvestDate == new DateOnly(2027, 7, 10) &&
                    command.ActualTonnes == 824.5m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new CropCyclesController(sender.Object);

        var result = await controller.Harvest(
            fieldId,
            cycleId,
            new HarvestCropCycleRequest(3, new DateOnly(2027, 7, 10), 824.5m),
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task CreateReturnsLocationForNewCycleOverview()
    {
        var sender = new Mock<ISender>();
        var fieldId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var expected = CreateDetails(fieldId, cycleId);
        sender.Setup(service => service.Send(It.IsAny<CreateCropCycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new CropCyclesController(sender.Object);

        var result = await controller.Create(
            fieldId,
            new CreateCropCycleRequest(
                "PlantCane", null, Guid.NewGuid(), new DateOnly(2026, 8, 1),
                new DateOnly(2027, 7, 1), new DateOnly(2027, 8, 31), 950m),
            CancellationToken.None);

        result.Result.ShouldBeOfType<CreatedAtActionResult>().RouteValues!["cropCycleId"].ShouldBe(cycleId);
    }

    private static CropCycleDetailsDto CreateDetails(Guid fieldId, Guid cycleId) => new(
        new CropCycleFieldDto(fieldId, "A-01", "North block", 12.5m),
        new CropCycleListItemDto(
            cycleId, "PlantCane", null, null, "N14", "2026-08-01", "2027-07-01",
            "2027-08-31", 950m, "ReadyForHarvest", 3, null),
        ["Harvest"],
        new Dictionary<string, string>(),
        []);
}
