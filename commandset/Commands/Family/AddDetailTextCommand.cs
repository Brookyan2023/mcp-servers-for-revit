using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class AddDetailTextCommand : ExternalEventCommandBase
{
    private AddDetailTextEventHandler HandlerImpl => (AddDetailTextEventHandler)Handler;
    public override string CommandName => "add_detail_text";

    public AddDetailTextCommand(UIApplication uiApp) : base(new AddDetailTextEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<TextNoteCreationInfo>>() ?? new List<TextNoteCreationInfo>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Add detail text operation timed out");
    }
}
