using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class AddReferencePlanesCommand : ExternalEventCommandBase
{
    private AddReferencePlanesEventHandler HandlerImpl => (AddReferencePlanesEventHandler)Handler;
    public override string CommandName => "add_reference_planes";

    public AddReferencePlanesCommand(UIApplication uiApp) : base(new AddReferencePlanesEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<ReferencePlaneInput>>() ?? new List<ReferencePlaneInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Add reference planes operation timed out");
    }
}
