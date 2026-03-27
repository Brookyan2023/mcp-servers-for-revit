using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class LoadFamilyIntoProjectCommand : ExternalEventCommandBase
{
    private LoadFamilyIntoProjectEventHandler HandlerImpl => (LoadFamilyIntoProjectEventHandler)Handler;
    public override string CommandName => "load_family_into_project";

    public LoadFamilyIntoProjectCommand(UIApplication uiApp) : base(new LoadFamilyIntoProjectEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var familyPath = parameters.Value<string>("familyPath");
        var targetProjectTitle = parameters.Value<string>("targetProjectTitle");
        var overwriteParameters = parameters.Value<bool?>("overwriteParameters") ?? true;
        HandlerImpl.SetParameters(familyPath, targetProjectTitle, overwriteParameters);
        if (RaiseAndWaitForCompletion(20000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Load family into project operation timed out");
    }
}
