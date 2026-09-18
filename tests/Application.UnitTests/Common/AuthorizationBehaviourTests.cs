using Ardalis.GuardClauses;
using Cane360.Application.Common.Behaviours;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Common;

public sealed class AuthorizationBehaviourTests
{
    [Test]
    public async Task GrowerRequestProceedsForActiveGrower()
    {
        var user = User("grower-user");
        var farms = Farms("grower-user", TenantSecurityRoles.Grower);
        var behaviour = Behaviour<DecidePayrollRunCommand>(user.Object, farms.Object);
        bool handled = false;

        string response = await behaviour.Handle(Request(), token =>
        {
            handled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        response.ShouldBe("allowed");
        handled.ShouldBeTrue();
        farms.Verify(repository => repository.GetActiveTenantSecurityRoleForUserAsync(
            "grower-user", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GrowerRequestRejectsActiveFarmManagerBeforeHandler()
    {
        var user = User("manager-user");
        var farms = Farms("manager-user", TenantSecurityRoles.FarmManager);
        var behaviour = Behaviour<DecidePayrollRunCommand>(user.Object, farms.Object);
        bool handled = false;

        await Should.ThrowAsync<ForbiddenAccessException>(() => behaviour.Handle(Request(), token =>
        {
            handled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None));

        handled.ShouldBeFalse();
    }

    [Test]
    public async Task GrowerRequestRejectsMissingActiveMembership()
    {
        var user = User("former-user");
        var farms = Farms("former-user", null);
        var behaviour = Behaviour<DecidePayrollRunCommand>(user.Object, farms.Object);

        await Should.ThrowAsync<NotFoundException>(() => behaviour.Handle(Request(),
            token => Task.FromResult("allowed"), CancellationToken.None));
    }

    [Test]
    public async Task GrowerRequestRejectsAnonymousUser()
    {
        var user = User(null);
        var farms = new Mock<IFarmSetupRepository>();
        var behaviour = Behaviour<DecidePayrollRunCommand>(user.Object, farms.Object);

        await Should.ThrowAsync<UnauthorizedAccessException>(() => behaviour.Handle(Request(),
            token => Task.FromResult("allowed"), CancellationToken.None));
        farms.VerifyNoOtherCalls();
    }

    private static DecidePayrollRunCommand Request() => new(Guid.NewGuid(), 1, 1, true, null, "test-key");

    private static AuthorizationBehaviour<TRequest, string> Behaviour<TRequest>(
        IUser user, IFarmSetupRepository farms) where TRequest : notnull =>
        new(user, new Mock<IIdentityService>().Object, farms);

    private static Mock<IUser> User(string? id)
    {
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns(id);
        return user;
    }

    private static Mock<IFarmSetupRepository> Farms(string userId, string? role)
    {
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(repository => repository.GetActiveTenantSecurityRoleForUserAsync(
            userId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        return farms;
    }
}
