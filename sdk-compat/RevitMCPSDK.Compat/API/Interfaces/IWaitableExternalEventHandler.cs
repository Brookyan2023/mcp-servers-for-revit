using Autodesk.Revit.UI;

namespace RevitMCPSDK.API.Interfaces;

public interface IWaitableExternalEventHandler : IExternalEventHandler
{
    bool WaitForCompletion(int timeoutMilliseconds = 10000);
}
