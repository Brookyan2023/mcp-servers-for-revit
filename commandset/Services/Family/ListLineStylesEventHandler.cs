using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class ListLineStylesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public object ResultInfo { get; private set; }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = FamilyEditorUtils.RequireActiveDocument(app);
            var lineCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            var styles = new List<(string Name, long Id)>();

            if (lineCategory != null)
            {
                foreach (Category subCategory in lineCategory.SubCategories)
                {
                    var style = subCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
                    styles.Add((subCategory.Name, style == null ? -1 : RevitInspectionUtils.IdValue(style.Id)));
                }
            }

            ResultInfo = new
            {
                count = styles.Count,
                styles = styles
                    .OrderBy(x => x.Name)
                    .Select(x => new { id = x.Id, name = x.Name })
                    .ToList(),
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "List Line Styles";
}
