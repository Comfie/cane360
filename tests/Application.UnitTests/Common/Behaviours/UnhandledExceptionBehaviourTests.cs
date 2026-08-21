using Cane360.Application.Common.Behaviours;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Common.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    [Test]
    public async Task ConflictIsNotLoggedAsAnUnhandledError()
    {
        var logger = new Mock<ILogger<TestRequest>>();
        var user = new Mock<IUser>();
        user.Setup(item => item.CorrelationId).Returns("test-correlation");
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger.Object, user.Object);

        await Should.ThrowAsync<ConflictException>(() => behaviour.Handle(
            new TestRequest(), _ => Task.FromException<string>(new ConflictException("Expected conflict.")),
            CancellationToken.None));

        logger.Verify(item => item.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((_, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        logger.Verify(item => item.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((_, _) => true),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task ValidationFailureIsNotLoggedAsAnUnhandledError()
    {
        var logger = new Mock<ILogger<TestRequest>>();
        var user = new Mock<IUser>();
        user.Setup(item => item.CorrelationId).Returns("test-correlation");
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger.Object, user.Object);

        await Should.ThrowAsync<ValidationException>(() => behaviour.Handle(
            new TestRequest(), _ => Task.FromException<string>(new ValidationException()), CancellationToken.None));

        logger.Verify(item => item.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((_, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        logger.Verify(item => item.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((_, _) => true),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    public sealed record TestRequest;
}
