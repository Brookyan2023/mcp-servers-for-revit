using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services;

public class SetElementParameterEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    public long ElementId { get; private set; }
    public string ParameterName { get; private set; } = string.Empty;
    public object Value { get; private set; }
    public string ValueUnit { get; private set; } = string.Empty;
    public object ResultInfo { get; private set; }
    private readonly ManualResetEvent _resetEvent = new(false);

    public void SetParameters(long elementId, string parameterName, object value, string valueUnit)
    {
        ElementId = elementId;
        ParameterName = parameterName ?? string.Empty;
        Value = value;
        ValueUnit = valueUnit ?? string.Empty;
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
            var doc = app.ActiveUIDocument.Document;
            var element = doc.GetElement(new ElementId(ElementId));
            if (element == null) throw new InvalidOperationException("Element not found");

            var parameter = RevitInspectionUtils.FindParameter(element, ParameterName);
            if (parameter == null) throw new InvalidOperationException("Parameter not found");
            if (parameter.IsReadOnly) throw new InvalidOperationException("Parameter is read-only");

            using (var transaction = new Transaction(doc, "MCP Set Element Parameter"))
            {
                transaction.Start();
                if (!RevitInspectionUtils.TrySetParameterValue(parameter, Value, ValueUnit, out var error))
                {
                    transaction.RollBack();
                    throw new InvalidOperationException(error ?? "Failed to set parameter");
                }
                transaction.Commit();
            }

            ResultInfo = new
            {
                element_id = ElementId,
                parameter = parameter.Definition.Name,
                value = RevitInspectionUtils.GetParameterValue(parameter),
                display_value = parameter.AsValueString(),
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Set Element Parameter";
}
