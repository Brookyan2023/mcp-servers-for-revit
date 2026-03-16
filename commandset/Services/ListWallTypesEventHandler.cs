using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class ListWallTypesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public object ResultInfo { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var rows = new FilteredElementCollector(app.ActiveUIDocument.Document)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Select(type => new
                {
                    id = RevitInspectionUtils.IdValue(type.Id),
                    name = RevitInspectionUtils.SafeName(type),
                    kind = type.Kind.ToString(),
                    width_mm = RevitInspectionUtils.ToMillimeters(type.Width),
                })
                .OrderBy(x => x.name)
                .ToList();

            ResultInfo = new { count = rows.Count, wall_types = rows };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "List Wall Types";
}
