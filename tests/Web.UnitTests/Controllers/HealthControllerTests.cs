using Cane360.Web.Controllers;
using Cane360.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class HealthControllerTests
{
    private Mock<IDatabaseHealthCheck> _healthCheck = null!;
    private HealthController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _healthCheck = new Mock<IDatabaseHealthCheck>();
        _controller = new HealthController(_healthCheck.Object);
    }

    [Test]
    public async Task GetReturnsOkWhenDatabaseIsAvailable()
    {
        _healthCheck
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Get(CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
    }

    [Test]
    public async Task GetReturnsServiceUnavailableWhenDatabaseIsUnavailable()
    {
        _healthCheck
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Get(CancellationToken.None);

        var unavailable = result.ShouldBeOfType<ObjectResult>();
        unavailable.StatusCode.ShouldBe(503);
    }
}
