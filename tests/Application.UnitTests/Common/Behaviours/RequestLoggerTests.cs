using Cane360.Application.Common.Behaviours;
using Cane360.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Cane360.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<TestRequest>> _logger = null!;
    private Mock<IUser> _user = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestRequest>>();
        _user = new Mock<IUser>();
    }

    [Test]
    public async Task ProcessLogsRequestTypeAndCorrelationWithoutUserIdentity()
    {
        _user.Setup(x => x.Id).Returns("private-user-id");
        _user.Setup(x => x.CorrelationId).Returns("safe-correlation");

        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object);

        await requestLogger.Process(new TestRequest(), CancellationToken.None);

        _logger.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) =>
                state.ToString()!.Contains(nameof(TestRequest), StringComparison.Ordinal) &&
                state.ToString()!.Contains("safe-correlation", StringComparison.Ordinal) &&
                !state.ToString()!.Contains("private-user-id", StringComparison.Ordinal)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task ProcessLogsUnauthenticatedRequestWithoutIdentityLookup()
    {
        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object);

        await requestLogger.Process(new TestRequest(), CancellationToken.None);

        _logger.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(nameof(TestRequest), StringComparison.Ordinal)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    public sealed record TestRequest : IRequest;
}
