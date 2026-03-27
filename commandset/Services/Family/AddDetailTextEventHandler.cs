using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Annotation;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class AddDetailTextEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<TextNoteCreationInfo> Notes { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<TextNoteCreationInfo> notes)
    {
        Notes = notes ?? new List<TextNoteCreationInfo>();
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

            using var tx = new Transaction(doc, "Add Detail Text");
            tx.Start();

            foreach (var note in Notes)
            {
                var location = JZPoint.ToXYZ(note.Location);
                var type = FamilyEditorUtils.FindTextNoteType(doc, note.TextNoteTypeId)
                    ?? throw new InvalidOperationException("No text note type is available in the active document.");

                var options = new TextNoteOptions(type.Id)
                {
                    HorizontalAlignment = ResolveHorizontalAlignment(note.HorizontalAlign)
                };

                var textNote = TextNote.Create(doc, view.Id, location, note.Text ?? string.Empty, options);
                if (Math.Abs(note.Rotation) > 1e-9)
                {
                    var axis = Line.CreateBound(location, location + view.ViewDirection);
                    ElementTransformUtils.RotateElement(doc, textNote.Id, axis, note.Rotation * Math.PI / 180.0);
                }

                created.Add(new
                {
                    id = RevitInspectionUtils.IdValue(textNote.Id),
                    text = note.Text ?? string.Empty,
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

    public string GetName() => "Add Detail Text";

    private static HorizontalTextAlignment ResolveHorizontalAlignment(int value)
    {
        return value switch
        {
            1 => HorizontalTextAlignment.Center,
            2 => HorizontalTextAlignment.Right,
            _ => HorizontalTextAlignment.Left,
        };
    }
}
