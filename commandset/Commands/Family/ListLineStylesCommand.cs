using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class ListLineStylesCommand : ExternalEventCommandBase
{
    private ListLineStylesEventHandler HandlerImpl => (ListLineStylesEventHandler)Handler;
    public override string CommandName => "list_line_styles";

    public ListLineStylesCommand(UIApplication uiApp) : base(new ListLineStylesEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("List line styles operation timed out");
    }
}
