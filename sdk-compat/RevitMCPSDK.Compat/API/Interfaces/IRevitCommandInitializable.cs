using Autodesk.Revit.UI;

namespace RevitMCPSDK.API.Interfaces;

public interface IRevitCommandInitializable
{
    void Initialize(UIApplication uiApplication);
}
