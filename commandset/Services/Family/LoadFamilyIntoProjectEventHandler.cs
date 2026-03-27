using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;
using System.IO;

namespace RevitMCPCommandSet.Services.Family;

public class LoadFamilyIntoProjectEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public string FamilyPath { get; private set; }
    public string TargetProjectTitle { get; private set; }
    public bool OverwriteParameters { get; private set; }
    public object ResultInfo { get; private set; }

    public void SetParameters(string familyPath, string targetProjectTitle, bool overwriteParameters)
    {
        FamilyPath = familyPath;
        TargetProjectTitle = targetProjectTitle;
        OverwriteParameters = overwriteParameters;
        _resetEvent.Reset();
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 20000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var activeDoc = FamilyEditorUtils.RequireActiveDocument(app);
            var targetProject = ResolveTargetProject(app, activeDoc);
            if (targetProject == null)
                throw new InvalidOperationException("No target project document is available for family loading.");

            Autodesk.Revit.DB.Family loadedFamily = null;
            var loadOptions = new AlwaysLoadFamilyOptions(OverwriteParameters);

            if (activeDoc.IsFamilyDocument)
            {
                loadedFamily = activeDoc.LoadFamily(targetProject, loadOptions);
                if (loadedFamily == null)
                    throw new InvalidOperationException("Revit did not load the active family document into the target project.");
                loadedFamily ??= FindFamilyByName(targetProject, activeDoc.OwnerFamily?.Name);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FamilyPath) || !File.Exists(FamilyPath))
                    throw new FileNotFoundException("Family file was not found.", FamilyPath);

                if (!targetProject.LoadFamily(FamilyPath, loadOptions, out loadedFamily))
                    throw new InvalidOperationException("Revit did not load the specified .rfa file into the target project.");
            }

            ResultInfo = new
            {
                target_project_title = targetProject.Title,
                family_name = loadedFamily?.Name ?? string.Empty,
                family_id = loadedFamily == null ? -1 : RevitInspectionUtils.IdValue(loadedFamily.Id),
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Load Family Into Project";

    private Document ResolveTargetProject(UIApplication app, Document activeDoc)
    {
        if (!string.IsNullOrWhiteSpace(TargetProjectTitle))
        {
            foreach (Document doc in app.Application.Documents)
            {
                if (!doc.IsFamilyDocument && string.Equals(doc.Title, TargetProjectTitle, StringComparison.OrdinalIgnoreCase))
                    return doc;
            }
        }

        if (!activeDoc.IsFamilyDocument)
            return activeDoc;

        foreach (Document doc in app.Application.Documents)
        {
            if (!doc.IsFamilyDocument)
                return doc;
        }

        return null;
    }

    private static Autodesk.Revit.DB.Family FindFamilyByName(Document doc, string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            return null;

        return new FilteredElementCollector(doc)
            .OfClass(typeof(Autodesk.Revit.DB.Family))
            .Cast<Autodesk.Revit.DB.Family>()
            .FirstOrDefault(x => string.Equals(x.Name, familyName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class AlwaysLoadFamilyOptions : IFamilyLoadOptions
    {
        private readonly bool _overwriteParameters;

        public AlwaysLoadFamilyOptions(bool overwriteParameters)
        {
            _overwriteParameters = overwriteParameters;
        }

        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = _overwriteParameters;
            return true;
        }

        public bool OnSharedFamilyFound(Autodesk.Revit.DB.Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = _overwriteParameters;
            return true;
        }
    }
}
