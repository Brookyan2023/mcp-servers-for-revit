using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;
using System.IO;

namespace RevitMCPCommandSet.Services.Family;

public class SaveFamilyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public string SavePath { get; private set; }
    public bool Overwrite { get; private set; }
    public object ResultInfo { get; private set; }

    public void SetParameters(string savePath, bool overwrite)
    {
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
            var doc = FamilyEditorUtils.RequireActiveFamilyDocument(app);
            var targetPath = string.IsNullOrWhiteSpace(SavePath) ? doc.PathName : SavePath;
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new InvalidOperationException("The active family has no saved path. Provide savePath.");

            var directory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("The provided savePath is invalid.");

            Directory.CreateDirectory(directory);

            if (!string.IsNullOrWhiteSpace(doc.PathName) &&
                string.Equals(Path.GetFullPath(doc.PathName), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
            {
                doc.Save();
            }
            else
            {
                var options = new SaveAsOptions { OverwriteExistingFile = Overwrite };
                doc.SaveAs(targetPath, options);
            }

            ResultInfo = new
            {
                title = doc.Title,
                save_path = targetPath,
                is_modified = doc.IsModified,
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Save Family";
}
