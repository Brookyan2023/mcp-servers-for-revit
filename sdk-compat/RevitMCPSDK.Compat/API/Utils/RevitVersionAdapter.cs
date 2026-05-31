using Autodesk.Revit.ApplicationServices;

namespace RevitMCPSDK.API.Utils;

public class RevitVersionAdapter
{
    private readonly Application _application;

    public RevitVersionAdapter(Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public string GetRevitVersion()
    {
        var value = _application.VersionNumber;
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim();
    }

    public bool IsVersionSupported(string[] supportedVersions)
    {
        if (supportedVersions == null || supportedVersions.Length == 0) return true;
        var current = GetRevitVersion();
        return supportedVersions.Any(v => string.Equals((v ?? string.Empty).Trim(), current, StringComparison.OrdinalIgnoreCase));
    }
}
