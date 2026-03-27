using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class CloseFamilyDocCommand : ExternalEventCommandBase
{
    private CloseFamilyDocEventHandler HandlerImpl => (CloseFamilyDocEventHandler)Handler;
    public override string CommandName => "close_family_doc";

    public CloseFamilyDocCommand(UIApplication uiApp) : base(new CloseFamilyDocEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var saveBeforeClose = parameters.Value<bool?>("saveBeforeClose") ?? false;
        var targetProjectTitle = parameters.Value<string>("targetProjectTitle");
        HandlerImpl.SetParameters(saveBeforeClose, targetProjectTitle);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Close family document operation timed out");
    }
}
