using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class AddFamilyDimensionCommand : ExternalEventCommandBase
{
    private AddFamilyDimensionEventHandler HandlerImpl => (AddFamilyDimensionEventHandler)Handler;
    public override string CommandName => "add_family_dimension";

    public AddFamilyDimensionCommand(UIApplication uiApp) : base(new AddFamilyDimensionEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<FamilyDimensionInput>>() ?? new List<FamilyDimensionInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Add family dimension operation timed out");
    }
}
