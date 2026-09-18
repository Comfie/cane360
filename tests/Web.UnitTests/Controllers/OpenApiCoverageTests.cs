using Cane360.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class OpenApiCoverageTests
{
    [Test]
    public void OperationalControllersDocumentEveryAction()
    {
        Type[] controllers =
        [
            typeof(AdministrationController),
            typeof(PayrollController),
            typeof(InventoryController),
            typeof(WorkerRatesController)
        ];
        var actions = controllers.SelectMany(controller => controller.GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), false).Length > 0))
            .ToArray();

        actions.Length.ShouldBe(88);
        foreach (var action in actions)
        {
            var attributes = action.GetCustomAttributes(false);
            attributes.Any(attribute => attribute.GetType().Name == "EndpointSummaryAttribute")
                .ShouldBeTrue($"{action.DeclaringType?.Name}.{action.Name} needs a summary.");
            attributes.Any(attribute => attribute.GetType().Name == "EndpointDescriptionAttribute")
                .ShouldBeTrue($"{action.DeclaringType?.Name}.{action.Name} needs a description.");
            attributes.OfType<ProducesResponseTypeAttribute>()
                .Any(attribute => attribute.StatusCode is >= 200 and < 300)
                .ShouldBeTrue($"{action.DeclaringType?.Name}.{action.Name} needs a success response.");
        }
    }

    [TestCase(typeof(PayrollController), nameof(PayrollController.CreateAdvance), StatusCodes.Status201Created)]
    [TestCase(typeof(PayrollController), nameof(PayrollController.CreateRun), StatusCodes.Status201Created)]
    [TestCase(typeof(InventoryController), nameof(InventoryController.CreateReceipt), StatusCodes.Status201Created)]
    [TestCase(typeof(AdministrationController), nameof(AdministrationController.DisableManager), StatusCodes.Status200OK)]
    public void DocumentedSuccessMatchesCurrentActionResult(Type controller, string actionName,
        int statusCode)
    {
        var action = controller.GetMethod(actionName)!;

        action.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .Cast<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode == statusCode).ShouldBeTrue();
    }
}
