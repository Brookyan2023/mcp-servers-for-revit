using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class DrawDetailArcCommand : ExternalEventCommandBase
{
    private DrawDetailArcEventHandler HandlerImpl => (DrawDetailArcEventHandler)Handler;
    public override string CommandName => "draw_detail_arc";

    public DrawDetailArcCommand(UIApplication uiApp) : base(new DrawDetailArcEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<DetailArcInput>>() ?? new List<DetailArcInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Draw detail arc operation timed out");
    }
}
