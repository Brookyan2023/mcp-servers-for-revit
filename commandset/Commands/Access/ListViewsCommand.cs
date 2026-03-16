using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class ListViewsCommand : ExternalEventCommandBase
{
    private ListViewsEventHandler HandlerImpl => (ListViewsEventHandler)Handler;
    public override string CommandName => "list_views";

    public ListViewsCommand(UIApplication uiApp) : base(new ListViewsEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        HandlerImpl.SetParameters(
            parameters?["view_type"]?.Value<string>(),
            parameters?["include_templates"]?.Value<bool>() ?? false,
            parameters?["max_items"]?.Value<int>() ?? 500);

        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("List views operation timed out");
    }
}
