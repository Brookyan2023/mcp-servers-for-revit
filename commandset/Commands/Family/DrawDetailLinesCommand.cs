using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class DrawDetailLinesCommand : ExternalEventCommandBase
{
    private DrawDetailLinesEventHandler HandlerImpl => (DrawDetailLinesEventHandler)Handler;
    public override string CommandName => "draw_detail_lines";

    public DrawDetailLinesCommand(UIApplication uiApp) : base(new DrawDetailLinesEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<DetailLineInput>>() ?? new List<DetailLineInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Draw detail lines operation timed out");
    }
}
