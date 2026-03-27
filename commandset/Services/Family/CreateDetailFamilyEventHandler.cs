using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System.IO;

namespace RevitMCPCommandSet.Services.Family;

public class CreateDetailFamilyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);

    public string TemplatePath { get; private set; }
    public string SavePath { get; private set; }
    public bool Overwrite { get; private set; }
    public object ResultInfo { get; private set; }

    public void SetParameters(string templatePath, string savePath, bool overwrite)
    {
        TemplatePath = templatePath;
        SavePath = savePath;
        Overwrite = overwrite;
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
            if (string.IsNullOrWhiteSpace(TemplatePath) || !File.Exists(TemplatePath))
                throw new FileNotFoundException("Family template file was not found.", TemplatePath);
            if (string.IsNullOrWhiteSpace(SavePath))
                throw new InvalidOperationException("A savePath is required so the new family can be activated for follow-up tools.");

            var directory = Path.GetDirectoryName(SavePath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("The provided savePath is invalid.");

            Directory.CreateDirectory(directory);

            if (File.Exists(SavePath) && !Overwrite)
                throw new InvalidOperationException($"The target family already exists: {SavePath}");

            var familyDoc = app.Application.NewFamilyDocument(TemplatePath)
                ?? throw new InvalidOperationException("Revit could not create a family document from the selected template.");

            var saveOptions = new SaveAsOptions { OverwriteExistingFile = Overwrite };
            familyDoc.SaveAs(SavePath, saveOptions);
            app.OpenAndActivateDocument(SavePath);

            ResultInfo = new
            {
                template_path = TemplatePath,
                save_path = SavePath,
                title = familyDoc.Title,
                is_family_document = familyDoc.IsFamilyDocument,
                owner_family_name = familyDoc.OwnerFamily?.Name ?? string.Empty,
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Create Detail Family";
}
