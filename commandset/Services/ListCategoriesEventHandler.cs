using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services;

public class ListCategoriesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
            var rows = new List<object>();
            foreach (Category category in app.ActiveUIDocument.Document.Settings.Categories)
            {
                if (category == null) continue;
                rows.Add(new
                {
                    name = category.Name,
                    category_type = category.CategoryType.ToString(),
                    allows_bound_parameters = category.AllowsBoundParameters,
                    is_tag_category = category.IsTagCategory,
                    has_material_quantities = category.HasMaterialQuantities,
                });
            }

            ResultInfo = new { count = rows.Count, categories = rows };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "List Categories";
}
