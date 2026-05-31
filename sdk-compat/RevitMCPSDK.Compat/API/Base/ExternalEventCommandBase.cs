using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPSDK.API.Base;

public abstract class ExternalEventCommandBase : IRevitCommand, IRevitCommandInitializable
{
    private readonly object _sync = new();
    private ExternalEvent _externalEvent;
    protected IWaitableExternalEventHandler Handler { get; }
    protected UIApplication UiApplication { get; private set; }

    protected ExternalEventCommandBase(IWaitableExternalEventHandler handler, UIApplication uiApplication = null)
    {
        Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        UiApplication = uiApplication;
    }

    public abstract string CommandName { get; }

    public abstract object Execute(Newtonsoft.Json.Linq.JObject parameters, string requestId);

    public void Initialize(UIApplication uiApplication)
    {
        UiApplication = uiApplication;
        EnsureExternalEventCreated();
    }

    protected bool RaiseAndWaitForCompletion(int timeoutMilliseconds = 10000)
    {
        EnsureExternalEventCreated();

        _externalEvent.Raise();
        return Handler.WaitForCompletion(timeoutMilliseconds);
    }

    private void EnsureExternalEventCreated()
    {
        lock (_sync)
        {
            _externalEvent ??= ExternalEvent.Create(Handler);
        }
    }
}
