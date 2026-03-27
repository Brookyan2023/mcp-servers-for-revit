using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class DrawFilledRegionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<FilledRegionInput> Regions { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<FilledRegionInput> regions)
    {
        Regions = regions ?? new List<FilledRegionInput>();
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

            using var tx = new Transaction(doc, "Draw Filled Regions");
            tx.Start();

            foreach (var item in Regions)
            {
                if (item.Boundary == null || item.Boundary.Count < 3)
                    throw new InvalidOperationException("Filled regions require at least three boundary points.");

                var loop = new CurveLoop();
                var points = item.Boundary.Select(JZPoint.ToXYZ).ToList();
                for (var i = 0; i < points.Count; i++)
                {
                    var start = points[i];
                    var end = points[(i + 1) % points.Count];
                    loop.Append(Line.CreateBound(start, end));
                }

                var regionType = FamilyEditorUtils.FindFilledRegionType(doc, item.FillPatternName)
                    ?? throw new InvalidOperationException("No filled region type is available in the active family document.");

                var region = FilledRegion.Create(doc, regionType.Id, view.Id, new List<CurveLoop> { loop });

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(region.Id),
                    fill_pattern_name = regionType.Name,
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

    public string GetName() => "Draw Filled Region";
}
