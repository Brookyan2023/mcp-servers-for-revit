using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class CloseFamilyDocEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public bool SaveBeforeClose { get; private set; }
    public string TargetProjectTitle { get; private set; }
    public object ResultInfo { get; private set; }

    public void SetParameters(bool saveBeforeClose, string targetProjectTitle)
    {
        SaveBeforeClose = saveBeforeClose;
        TargetProjectTitle = targetProjectTitle;
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
            var targetProject = ResolveTargetProject(app, doc, TargetProjectTitle);
            if (targetProject == null || string.IsNullOrWhiteSpace(targetProject.PathName))
                throw new InvalidOperationException("A saved project document must be open so Revit can return focus before closing the active family.");

            var familyTitle = doc.Title;
            var familyPath = doc.PathName ?? string.Empty;
            app.OpenAndActivateDocument(targetProject.PathName);
            doc.Close(SaveBeforeClose);

            ResultInfo = new
            {
                closed_family_title = familyTitle,
                closed_family_path = familyPath,
                active_project_title = targetProject.Title,
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Close Family Doc";

    private static Autodesk.Revit.DB.Document ResolveTargetProject(UIApplication app, Autodesk.Revit.DB.Document familyDoc, string targetProjectTitle)
    {
        foreach (Autodesk.Revit.DB.Document doc in app.Application.Documents)
        {
            if (doc.Equals(familyDoc) || doc.IsFamilyDocument)
                continue;

            if (!string.IsNullOrWhiteSpace(targetProjectTitle) &&
                !string.Equals(doc.Title, targetProjectTitle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(doc.PathName) &&
                (string.IsNullOrWhiteSpace(familyDoc.Title) || string.Equals(doc.Title, familyDoc.Title, StringComparison.OrdinalIgnoreCase) == false))
            {
                return doc;
            }
        }

        return null;
    }
}
