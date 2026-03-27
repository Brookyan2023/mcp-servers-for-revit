using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class DrawDetailLinesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<DetailLineInput> Lines { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<DetailLineInput> lines)
    {
        Lines = lines ?? new List<DetailLineInput>();
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

            using var tx = new Transaction(doc, "Draw Detail Lines");
            tx.Start();

            foreach (var item in Lines)
            {
                var curve = JZLine.ToLine(item.Line) ?? throw new InvalidOperationException("Detail line input is missing a valid line.");
                var detailCurve = doc.FamilyCreate.NewDetailCurve(view, curve);

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

    public string GetName() => "Draw Detail Lines";
}
