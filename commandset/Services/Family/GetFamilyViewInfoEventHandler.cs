using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class GetFamilyViewInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
            var view = doc.ActiveView;

            ResultInfo = new
            {
                title = doc.Title,
                path_name = doc.PathName ?? string.Empty,
                is_family_document = doc.IsFamilyDocument,
                owner_family = doc.OwnerFamily == null ? null : new
                {
                    id = RevitInspectionUtils.IdValue(doc.OwnerFamily.Id),
                    name = RevitInspectionUtils.SafeName(doc.OwnerFamily),
                    category = doc.OwnerFamily.FamilyCategory?.Name ?? string.Empty,
                },
                active_view = view == null ? null : new
                {
                    id = RevitInspectionUtils.IdValue(view.Id),
                    name = view.Name,
                    view_type = view.ViewType.ToString(),
                    scale = view.Scale,
                    is_template = view.IsTemplate,
                },
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Get Family View Info";
}
