using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class GetDocumentInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;
            var activeView = uiDoc.ActiveView;

            ResultInfo = new
            {
                title = doc.Title,
                path_name = doc.PathName ?? string.Empty,
                is_family_document = doc.IsFamilyDocument,
                is_workshared = doc.IsWorkshared,
                is_modified = doc.IsModified,
                active_view = activeView == null ? null : new
                {
                    id = RevitInspectionUtils.IdValue(activeView.Id),
                    name = RevitInspectionUtils.SafeName(activeView),
                    view_type = activeView.ViewType.ToString(),
                },
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Get Document Info";
}
