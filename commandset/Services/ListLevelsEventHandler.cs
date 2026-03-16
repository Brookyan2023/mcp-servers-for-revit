using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class ListLevelsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Select(level => new
                {
                    id = RevitInspectionUtils.IdValue(level.Id),
                    name = RevitInspectionUtils.SafeName(level),
                    elevation_mm = RevitInspectionUtils.ToMillimeters(level.Elevation),
                })
                .OrderBy(x => x.elevation_mm)
                .ToList();

            ResultInfo = new { count = rows.Count, levels = rows };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "List Levels";
}
