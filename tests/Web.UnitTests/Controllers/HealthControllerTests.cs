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

    [Test]
    public void GetLiveDoesNotQueryDatabase()
    {
        var result = _controller.GetLive();

        result.ShouldBeOfType<OkObjectResult>();
        _healthCheck.Verify(x => x.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetReadyReturnsServiceUnavailableWithoutLeakingDependencyException()
    {
        _healthCheck
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private database endpoint"));

        var result = await _controller.GetReady(CancellationToken.None);

        var unavailable = result.ShouldBeOfType<ObjectResult>();
        unavailable.StatusCode.ShouldBe(503);
        string response = unavailable.Value?.ToString() ?? string.Empty;
        response.ShouldNotContain("private database endpoint");
    }
}
