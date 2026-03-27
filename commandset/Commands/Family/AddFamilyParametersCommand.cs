using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class AddFamilyParametersCommand : ExternalEventCommandBase
{
    private AddFamilyParametersEventHandler HandlerImpl => (AddFamilyParametersEventHandler)Handler;
    public override string CommandName => "add_family_parameters";

    public AddFamilyParametersCommand(UIApplication uiApp) : base(new AddFamilyParametersEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<FamilyParameterDefinition>>() ?? new List<FamilyParameterDefinition>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Add family parameters operation timed out");
    }
}
