using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class GetFamilyViewInfoCommand : ExternalEventCommandBase
{
    private GetFamilyViewInfoEventHandler HandlerImpl => (GetFamilyViewInfoEventHandler)Handler;
    public override string CommandName => "get_family_view_info";

    public GetFamilyViewInfoCommand(UIApplication uiApp) : base(new GetFamilyViewInfoEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Get family view info operation timed out");
    }
}
