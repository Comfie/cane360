using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class ManagerInvitationDomainTests
{
    [Test]
    public void InvitationIsSingleUseRevocableAndExpires()
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = ManagerInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new string('A', 64), now.AddHours(1), "grower-user");
        invitation.Redeem(now, "manager-user");
        Should.Throw<InvalidOperationException>(() => invitation.Redeem(now, "another-user"));
        Should.Throw<InvalidOperationException>(() => invitation.Revoke(now, "grower-user", invitation.Version));

        var revoked = ManagerInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new string('B', 64), now.AddHours(1), "grower-user");
        revoked.Revoke(now, "grower-user", revoked.Version);
        Should.Throw<InvalidOperationException>(() => revoked.Redeem(now, "manager-user"));

        var expired = ManagerInvitation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new string('C', 64), now.AddMinutes(-1), "grower-user");
        Should.Throw<InvalidOperationException>(() => expired.Redeem(now, "manager-user"));
    }

    [Test]
    public void TokenHashIsTheOnlyPersistedTokenMaterial()
    {
        typeof(ManagerInvitation).GetProperties().Select(property => property.Name)
            .ShouldNotContain("Token");
        typeof(ManagerInvitation).GetProperties().Select(property => property.Name)
            .ShouldContain("TokenHash");
    }
}
