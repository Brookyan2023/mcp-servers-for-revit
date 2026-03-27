using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class AddSymbolicLineEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<SymbolicLineInput> Lines { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<SymbolicLineInput> lines)
    {
        Lines = lines ?? new List<SymbolicLineInput>();
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

            using var tx = new Transaction(doc, "Add Symbolic Lines");
            tx.Start();

            foreach (var item in Lines)
            {
                var curve = JZLine.ToLine(item.Line) ?? throw new InvalidOperationException("Symbolic line input is missing a valid line.");
                var sketchPlane = FamilyEditorUtils.CreateViewSketchPlane(doc, view, curve.GetEndPoint(0));
                var symbolicCurve = doc.FamilyCreate.NewSymbolicCurve(curve, sketchPlane);

                var style = FamilyEditorUtils.FindLineStyle(doc, item.LineStyle);
                if (style != null)
                    symbolicCurve.LineStyle = style;

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(symbolicCurve.Id),
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

    public string GetName() => "Add Symbolic Line";
}
