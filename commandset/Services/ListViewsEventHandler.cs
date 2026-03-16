using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class ListViewsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public string ViewTypeFilter { get; private set; } = string.Empty;
    public bool IncludeTemplates { get; private set; }
    public int MaxItems { get; private set; } = 500;
    public object ResultInfo { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public void SetParameters(string viewTypeFilter, bool includeTemplates, int maxItems)
    {
        ViewTypeFilter = viewTypeFilter ?? string.Empty;
        IncludeTemplates = includeTemplates;
        MaxItems = maxItems > 0 ? maxItems : 500;
        _resetEvent.Reset();
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var rows = new List<object>();
            foreach (View view in new FilteredElementCollector(app.ActiveUIDocument.Document).OfClass(typeof(View)))
            {
                if (!IncludeTemplates && view.IsTemplate) continue;
                if (!string.IsNullOrWhiteSpace(ViewTypeFilter) &&
                    !string.Equals(view.ViewType.ToString(), ViewTypeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                rows.Add(new
                {
                    id = RevitInspectionUtils.IdValue(view.Id),
                    name = RevitInspectionUtils.SafeName(view),
                    view_type = view.ViewType.ToString(),
                    is_template = view.IsTemplate,
                });

                if (rows.Count >= MaxItems) break;
            }

            ResultInfo = new { count = rows.Count, view_type_filter = ViewTypeFilter, include_templates = IncludeTemplates, views = rows };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "List Views";
}
