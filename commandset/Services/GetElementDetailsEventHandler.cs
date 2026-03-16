using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class GetElementDetailsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public long ElementId { get; private set; }
    public object ResultInfo { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public void SetParameters(long elementId)
    {
        ElementId = elementId;
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
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;
            var element = doc.GetElement(new ElementId(ElementId));
            if (element == null) throw new InvalidOperationException("Element not found");

            var type = doc.GetElement(element.GetTypeId());
            var boundingBox = element.get_BoundingBox(uiDoc.ActiveView) ?? element.get_BoundingBox(null);

            ResultInfo = new
            {
                element = new
                {
                    id = RevitInspectionUtils.IdValue(element.Id),
                    name = RevitInspectionUtils.SafeName(element),
                    unique_id = element.UniqueId,
                    category = element.Category?.Name,
                    class_name = element.GetType().Name,
                    type_id = RevitInspectionUtils.IdValue(element.GetTypeId()),
                    type_name = RevitInspectionUtils.SafeName(type),
                    level = RevitInspectionUtils.LevelName(doc, RevitInspectionUtils.TryGetLevelId(element)),
                    location = RevitInspectionUtils.DescribeLocation(element.Location),
                    bounding_box = RevitInspectionUtils.DescribeBoundingBox(boundingBox),
                }
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Get Element Details";
}
