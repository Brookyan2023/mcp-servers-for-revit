using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class AddFamilyDimensionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<FamilyDimensionInput> Dimensions { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<FamilyDimensionInput> dimensions)
    {
        Dimensions = dimensions ?? new List<FamilyDimensionInput>();
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
            var familyManager = doc.FamilyManager ?? throw new InvalidOperationException("FamilyManager is unavailable in the active family document.");
            var created = new List<object>();

            using var tx = new Transaction(doc, "Add Family Dimensions");
            tx.Start();

            foreach (var item in Dimensions)
            {
                if (item.ReferencePlaneNames == null || item.ReferencePlaneNames.Count < 2)
                    throw new InvalidOperationException("Family dimensions require at least two reference plane names.");

                var references = new ReferenceArray();
                foreach (var planeName in item.ReferencePlaneNames)
                {
                    var plane = FamilyEditorUtils.FindReferencePlane(doc, planeName)
                        ?? throw new InvalidOperationException($"Reference plane '{planeName}' was not found.");
                    references.Append(plane.GetReference());
                }

                var line = JZLine.ToLine(item.Line) ?? throw new InvalidOperationException("Dimension line is invalid.");
                var dimension = doc.FamilyCreate.NewLinearDimension(view, line, references);

                if (!string.IsNullOrWhiteSpace(item.LabelParameterName))
                {
                    var parameter = FamilyEditorUtils.FindFamilyParameter(familyManager, item.LabelParameterName)
                        ?? throw new InvalidOperationException($"Family parameter '{item.LabelParameterName}' was not found.");
                    dimension.FamilyLabel = parameter;
                }

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(dimension.Id),
                    label_parameter_name = item.LabelParameterName ?? string.Empty,
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

    public string GetName() => "Add Family Dimension";
}
