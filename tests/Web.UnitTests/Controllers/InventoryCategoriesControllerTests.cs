using Cane360.Application.Inventory;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class InventoryCategoriesControllerTests
{
    [Test]
    public void CategoryControllerRequiresAuthenticationAndHasNoDeleteRoute()
    {
        typeof(InventoryCategoriesController).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        typeof(InventoryCategoriesController).GetMethods().ShouldAllBe(method =>
            method.GetCustomAttributes(typeof(HttpDeleteAttribute), true).Length == 0);
    }

    [Test]
    public async Task CreateMapsMetadataOnly()
    {
        Mock<ISender> sender = new();
        InventoryCategoryDto expected = new(Guid.NewGuid(), "FUEL", "Fuel", "Diesel", 5, true, 1);
        sender.Setup(value => value.Send(It.Is<CreateInventoryCategoryCommand>(command =>
            command.Code == "FUEL" && command.Name == "Fuel" && command.Description == "Diesel" && command.DisplayOrder == 5),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new InventoryCategoriesController(sender.Object).CreateInventoryCategory(
            new("FUEL", "Fuel", "Diesel", 5), default);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task UpdateMapsStableIdentityAndExpectedVersion()
    {
        Mock<ISender> sender = new(); Guid id = Guid.NewGuid();
        InventoryCategoryDto expected = new(id, "FUEL", "Renamed", null, 7, true, 2);
        sender.Setup(value => value.Send(It.Is<UpdateInventoryCategoryCommand>(command =>
            command.CategoryId == id && command.Name == "Renamed" && command.DisplayOrder == 7 && command.ExpectedVersion == 1),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new InventoryCategoriesController(sender.Object).UpdateInventoryCategory(id, new("Renamed", null, 7, 1), default);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task StatusMapsExplicitActiveStateAndVersion(bool active)
    {
        Mock<ISender> sender = new(); Guid id = Guid.NewGuid();
        InventoryCategoryDto expected = new(id, "FUEL", "Fuel", null, 0, active, 3);
        sender.Setup(value => value.Send(It.Is<SetInventoryCategoryActiveCommand>(command =>
            command.CategoryId == id && command.Active == active && command.ExpectedVersion == 2),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new InventoryCategoriesController(sender.Object).SetInventoryCategoryActive(id, new(active, 2), default);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }
}
