using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class ListCategoriesCommand : ExternalEventCommandBase
{
    private ListCategoriesEventHandler HandlerImpl => (ListCategoriesEventHandler)Handler;
    public override string CommandName => "list_categories";

    public ListCategoriesCommand(UIApplication uiApp) : base(new ListCategoriesEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("List categories operation timed out");
    }
}
