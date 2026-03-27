using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class AddReferencePlanesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<ReferencePlaneInput> Planes { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<ReferencePlaneInput> planes)
    {
        Planes = planes ?? new List<ReferencePlaneInput>();
        _resetEvent.Reset();
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 15000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = FamilyEditorUtils.RequireActiveFamilyDocument(app);
            var view = FamilyEditorUtils.RequireUsableView(doc);
            var created = new List<object>();

            using var tx = new Transaction(doc, "Add Reference Planes");
            tx.Start();

            foreach (var plane in Planes)
            {
                var referencePlane = doc.FamilyCreate.NewReferencePlane(
                    JZPoint.ToXYZ(plane.BubbleEnd),
                    JZPoint.ToXYZ(plane.FreeEnd),
                    JZPoint.ToXYZ(plane.CutVector),
                    view);

                if (!string.IsNullOrWhiteSpace(plane.Name))
                    referencePlane.Name = plane.Name;

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(referencePlane.Id),
                    name = referencePlane.Name,
                });
            }

            tx.Commit();

            ResultInfo = new
            {
                created_count = created.Count,
                created,
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Add Reference Planes";
}
