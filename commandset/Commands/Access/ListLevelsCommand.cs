using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class ListLevelsCommand : ExternalEventCommandBase
{
    private ListLevelsEventHandler HandlerImpl => (ListLevelsEventHandler)Handler;
    public override string CommandName => "list_levels";

    public ListLevelsCommand(UIApplication uiApp) : base(new ListLevelsEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("List levels operation timed out");
    }
}
