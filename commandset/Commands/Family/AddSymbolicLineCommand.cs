using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Services.Family;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Family;

public class AddSymbolicLineCommand : ExternalEventCommandBase
{
    private AddSymbolicLineEventHandler HandlerImpl => (AddSymbolicLineEventHandler)Handler;
    public override string CommandName => "add_symbolic_line";

    public AddSymbolicLineCommand(UIApplication uiApp) : base(new AddSymbolicLineEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        var data = parameters["data"]?.ToObject<List<SymbolicLineInput>>() ?? new List<SymbolicLineInput>();
        HandlerImpl.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000)) return HandlerImpl.ResultInfo;
        throw new TimeoutException("Add symbolic line operation timed out");
    }
}
