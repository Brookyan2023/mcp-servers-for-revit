using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class GetElementParametersCommand : ExternalEventCommandBase
{
    private GetElementParametersEventHandler HandlerImpl => (GetElementParametersEventHandler)Handler;
    public override string CommandName => "get_element_parameters";

    public GetElementParametersCommand(UIApplication uiApp) : base(new GetElementParametersEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var elementId = parameters?["element_id"]?.Value<long>() ?? 0;
        if (elementId <= 0) throw new ArgumentException("element_id is required");
        HandlerImpl.SetParameters(
            elementId,
            parameters?["name_filter"]?.Value<string>(),
            parameters?["include_read_only"]?.Value<bool>() ?? true);

        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Get element parameters operation timed out");
    }
}
