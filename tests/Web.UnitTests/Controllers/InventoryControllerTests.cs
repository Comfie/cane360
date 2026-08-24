using Cane360.Application.Inventory;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class InventoryControllerTests
{
    [Test]
    public void ControllerRequiresAuthentication()
    {
        typeof(InventoryController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .ShouldNotBeEmpty();
    }

    [Test]
    public async Task ReceiptRejectsNonIsoDateBeforeDispatch()
    {
        var sender = new Mock<ISender>();
        var controller = new InventoryController(sender.Object);
        var request = new CreateStockReceiptRequest(
            "Purchase", Guid.NewGuid(), "22/08/2026", null, "GRN-1", null, null,
            [new CreateStockReceiptLineRequest(Guid.NewGuid(), null, 10m, 2m)]);

        var result = await controller.CreateReceipt(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        sender.Verify(service => service.Send(
            It.IsAny<CreateStockReceiptCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PostMapsVersionAndIdempotencyKey()
    {
        var sender = new Mock<ISender>();
        var receiptId = Guid.NewGuid();
        var expected = new StockReceiptDto(
            receiptId, "Purchase", null, null, new DateOnly(2026, 8, 22), null, null,
            "GRN-1", null, null, "Posted", DateTimeOffset.UtcNow, null, 3, 20m, []);
        sender.Setup(service => service.Send(
                It.Is<PostStockReceiptCommand>(command =>
                    command.ReceiptId == receiptId && command.ExpectedVersion == 2 &&
                    command.IdempotencyKey == "post-key"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(sender.Object);

        var result = await controller.PostReceipt(
            receiptId, new PostStockReceiptRequest(2, "post-key"), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }
}
