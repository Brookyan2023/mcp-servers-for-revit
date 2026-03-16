using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access;

public class GetDocumentInfoCommand : ExternalEventCommandBase
{
    private GetDocumentInfoEventHandler HandlerImpl => (GetDocumentInfoEventHandler)Handler;
    public override string CommandName => "get_document_info";

    public GetDocumentInfoCommand(UIApplication uiApp) : base(new GetDocumentInfoEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(10000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Get document info operation timed out");
    }
}
