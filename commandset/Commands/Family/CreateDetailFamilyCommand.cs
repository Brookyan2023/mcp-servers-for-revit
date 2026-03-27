using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class CreateDetailFamilyCommand : ExternalEventCommandBase
{
    private CreateDetailFamilyEventHandler HandlerImpl => (CreateDetailFamilyEventHandler)Handler;
    public override string CommandName => "create_detail_family";

    public CreateDetailFamilyCommand(UIApplication uiApp) : base(new CreateDetailFamilyEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var templatePath = parameters.Value<string>("templatePath");
        var savePath = parameters.Value<string>("savePath");
        var overwrite = parameters.Value<bool?>("overwrite") ?? false;

        HandlerImpl.SetParameters(templatePath, savePath, overwrite);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Create detail family operation timed out");
    }
}
