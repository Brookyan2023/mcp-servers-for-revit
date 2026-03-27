using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class DrawDetailArcEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<DetailArcInput> Arcs { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<DetailArcInput> arcs)
    {
        Arcs = arcs ?? new List<DetailArcInput>();
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
            var xAxis = FamilyEditorUtils.ResolveViewRightDirection(view);
            var yAxis = FamilyEditorUtils.ResolveViewUpDirection(view);
            var created = new List<object>();

            using var tx = new Transaction(doc, "Draw Detail Arcs");
            tx.Start();

            foreach (var item in Arcs)
            {
                var center = JZPoint.ToXYZ(item.Center);
                var radius = UnitUtils.ConvertToInternalUnits(item.Radius, UnitTypeId.Millimeters);
                var startAngle = item.StartAngle * Math.PI / 180.0;
                var endAngle = item.EndAngle * Math.PI / 180.0;

                var arc = Arc.Create(center, radius, startAngle, endAngle, xAxis, yAxis);
                var detailCurve = doc.FamilyCreate.NewDetailCurve(view, arc);

                var style = FamilyEditorUtils.FindLineStyle(doc, item.LineStyle);
                if (style != null)
                    detailCurve.LineStyle = style;

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(detailCurve.Id),
                    line_style = item.LineStyle ?? string.Empty,
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

    public string GetName() => "Draw Detail Arc";
}
