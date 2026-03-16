using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class GetElementParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public long ElementId { get; private set; }
    public string NameFilter { get; private set; } = string.Empty;
    public bool IncludeReadOnly { get; private set; } = true;
    public object ResultInfo { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public void SetParameters(long elementId, string nameFilter, bool includeReadOnly)
    {
        ElementId = elementId;
        NameFilter = (nameFilter ?? string.Empty).Trim();
        IncludeReadOnly = includeReadOnly;
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
            var doc = app.ActiveUIDocument.Document;
            var element = doc.GetElement(new ElementId(ElementId));
            if (element == null) throw new InvalidOperationException("Element not found");

            var rows = new List<Dictionary<string, object>>();
            foreach (Parameter parameter in element.Parameters)
            {
                if (parameter?.Definition == null) continue;
                var definitionName = parameter.Definition.Name ?? string.Empty;
                if (!IncludeReadOnly && parameter.IsReadOnly) continue;
                if (NameFilter.Length > 0 && definitionName.IndexOf(NameFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                rows.Add(RevitInspectionUtils.SerializeParameter(parameter));
            }

            ResultInfo = new
            {
                element_id = ElementId,
                count = rows.Count,
                parameters = rows.OrderBy(x => x["name"]).ToList(),
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Get Element Parameters";
}
