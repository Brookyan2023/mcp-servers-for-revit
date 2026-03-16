using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class SetElementParameterCommand : ExternalEventCommandBase
{
    private SetElementParameterEventHandler HandlerImpl => (SetElementParameterEventHandler)Handler;
    public override string CommandName => "set_element_parameter";

    public SetElementParameterCommand(UIApplication uiApp) : base(new SetElementParameterEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var elementId = parameters?["element_id"]?.Value<long>() ?? 0;
        var parameterName = parameters?["parameter_name"]?.Value<string>();
        if (elementId <= 0) throw new ArgumentException("element_id is required");
        if (string.IsNullOrWhiteSpace(parameterName)) throw new ArgumentException("parameter_name is required");
        if (parameters?["value"] == null) throw new ArgumentException("value is required");

        HandlerImpl.SetParameters(
            elementId,
            parameterName,
            parameters["value"].ToObject<object>(),
            parameters?["value_unit"]?.Value<string>());

        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Set element parameter operation timed out");
    }
}
