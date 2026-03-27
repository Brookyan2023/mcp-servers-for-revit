using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class SaveFamilyCommand : ExternalEventCommandBase
{
    private SaveFamilyEventHandler HandlerImpl => (SaveFamilyEventHandler)Handler;
    public override string CommandName => "save_family";

    public SaveFamilyCommand(UIApplication uiApp) : base(new SaveFamilyEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var savePath = parameters.Value<string>("savePath");
        var overwrite = parameters.Value<bool?>("overwrite") ?? false;
        HandlerImpl.SetParameters(savePath, overwrite);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Save family operation timed out");
    }
}
