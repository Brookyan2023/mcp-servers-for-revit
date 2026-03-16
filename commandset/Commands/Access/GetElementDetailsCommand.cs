using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class GetElementDetailsCommand : ExternalEventCommandBase
{
    private GetElementDetailsEventHandler HandlerImpl => (GetElementDetailsEventHandler)Handler;
    public override string CommandName => "get_element_details";

    public GetElementDetailsCommand(UIApplication uiApp) : base(new GetElementDetailsEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var elementId = parameters?["element_id"]?.Value<long>() ?? 0;
        if (elementId <= 0) throw new ArgumentException("element_id is required");
        HandlerImpl.SetParameters(elementId);
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Get element details operation timed out");
    }
}
