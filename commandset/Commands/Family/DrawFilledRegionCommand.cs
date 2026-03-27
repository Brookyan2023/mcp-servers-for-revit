using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class DrawFilledRegionCommand : ExternalEventCommandBase
{
    private DrawFilledRegionEventHandler HandlerImpl => (DrawFilledRegionEventHandler)Handler;
    public override string CommandName => "draw_filled_region";

    public DrawFilledRegionCommand(UIApplication uiApp) : base(new DrawFilledRegionEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<FilledRegionInput>>() ?? new List<FilledRegionInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Draw filled region operation timed out");
    }
}
