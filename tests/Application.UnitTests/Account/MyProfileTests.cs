using Cane360.Application.Account;
using Cane360.Application.Common.Interfaces;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Account;

public sealed class MyProfileTests
{
    [Test]
    public async Task ReadsOnlyTheAuthenticatedAccount()
    {
        var user = new Mock<IUser>();
        user.SetupGet(item => item.Id).Returns("signed-in-user");
        var profiles = new Mock<IAccountProfileStore>(MockBehavior.Strict);
        var profile = new AccountProfileDto("synthetic@example.invalid", false, "Synthetic account", null);
        profiles.Setup(item => item.GetAsync("signed-in-user", It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await new GetMyProfileQueryHandler(profiles.Object, user.Object)
            .Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.ShouldBe(profile);
    }

    [Test]
    public async Task UpdatesOnlyTheAuthenticatedAccountAndAllowsClearingContactNumber()
    {
        var user = new Mock<IUser>();
        user.SetupGet(item => item.Id).Returns("signed-in-user");
        var profiles = new Mock<IAccountProfileStore>(MockBehavior.Strict);
        var profile = new AccountProfileDto("synthetic@example.invalid", false, "Synthetic account", null);
        profiles.Setup(item => item.UpdateAsync("signed-in-user", "Synthetic account", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await new UpdateMyProfileCommandHandler(profiles.Object, user.Object)
            .Handle(new UpdateMyProfileCommand("  Synthetic account  ", "  "), CancellationToken.None);

        result.ShouldBe(profile);
    }

    [Test]
    public async Task AnonymousRequestsCannotReadOrWriteProfiles()
    {
        var user = new Mock<IUser>();
        var profiles = new Mock<IAccountProfileStore>(MockBehavior.Strict);

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new GetMyProfileQueryHandler(profiles.Object, user.Object).Handle(new GetMyProfileQuery(), CancellationToken.None));
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new UpdateMyProfileCommandHandler(profiles.Object, user.Object)
                .Handle(new UpdateMyProfileCommand("Synthetic", null), CancellationToken.None));
        profiles.VerifyNoOtherCalls();
    }

    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase("Synthetic", "invalid phone")]
    public async Task RejectsInvalidPersonalDetails(string name, string? phone)
    {
        var result = await new UpdateMyProfileCommandValidator().ValidateAsync(new UpdateMyProfileCommand(name, phone));
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task AcceptsInternationalContactNumber()
    {
        var result = await new UpdateMyProfileCommandValidator()
            .ValidateAsync(new UpdateMyProfileCommand("Synthetic", "+27 (00) 123-4567"));
        result.IsValid.ShouldBeTrue();
    }
}
