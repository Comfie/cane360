using System.Text.Json;
using Cane360.Application.FarmSetup;
using Cane360.Web.Controllers;
using Cane360.Web.Models.FarmSetup;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class FarmProfileContractTests
{
    [Test]
    public async Task ExistingUpdateRouteForwardsOptionalEnhancements()
    {
        var sender = new Mock<ISender>();
        UpdateFarmInformationCommand? sent = null;
        sender.Setup(item => item.Send(It.IsAny<UpdateFarmInformationCommand>(), CancellationToken.None))
            .Callback<IRequest<FarmSetupDto>, CancellationToken>((request, _) => sent = (UpdateFarmInformationCommand)request)
            .ReturnsAsync(new FarmSetupDto(true, null, null));
        var profile = new FarmOwnerProfileInput(FirstName: "Synthetic", GrowerNumber: "G1");
        var modelId = Guid.NewGuid();
        await new FarmSetupController(sender.Object).UpdateFarm(new UpdateFarmInformationRequest(
            "Owner", null, "F1", "Farm", "Address", "Location", "Owned", 10, "Furrow", profile,
            modelId, true), CancellationToken.None);
        sent!.OwnerProfile.ShouldBe(profile);
        sent.FarmModelId.ShouldBe(modelId);
        sent.UpdateFarmModel.ShouldBeTrue();
    }

    [Test]
    public void LegacyRequestDeserializesWithoutNewRequiredFields()
    {
        var request = JsonSerializer.Deserialize<UpdateFarmInformationRequest>(
            """{"GrowerDisplayName":"Owner","FarmCode":"F1","FarmName":"Farm","Address":"Address","Location":"Location","Tenure":"Owned","DeclaredHectares":10,"IrrigationContext":"Furrow"}""");
        request!.OwnerProfile.ShouldBeNull();
        request.UpdateFarmModel.ShouldBeFalse();
    }

    [Test]
    public void EnhancedResponseOnlyExposesNationalIdMask()
    {
        string json = JsonSerializer.Serialize(new GrowerDto("Owner", null, FirstName: "Synthetic",
            NationalIdMask: "••••••12"), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldContain("firstName");
        json.ShouldContain("nationalIdMask");
        json.ShouldNotContain("ciphertext");
        json.ShouldNotContain("fingerprint");
    }

    [Test]
    public void NewEndpointsRequireAuthenticationAndRevealDisablesCaching()
    {
        typeof(FarmSetupController).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        var reveal = typeof(FarmSetupController).GetMethod(nameof(FarmSetupController.RevealFarmOwnerNationalId))!;
        reveal.GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>()
            .Single().NoStore.ShouldBeTrue();
    }
}
