using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class ListWallTypesCommand : ExternalEventCommandBase
{
    private ListWallTypesEventHandler HandlerImpl => (ListWallTypesEventHandler)Handler;
    public override string CommandName => "list_wall_types";

    public ListWallTypesCommand(UIApplication uiApp) : base(new ListWallTypesEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("List wall types operation timed out");
    }
}
