using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Family;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Services.Family;

public class AddFamilyParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new(false);
    public List<FamilyParameterDefinition> Parameters { get; private set; } = new();
    public object ResultInfo { get; private set; }

    public void SetParameters(List<FamilyParameterDefinition> parameters)
    {
        Parameters = parameters ?? new List<FamilyParameterDefinition>();
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
            var familyManager = doc.FamilyManager ?? throw new InvalidOperationException("FamilyManager is unavailable in the active family document.");
            var created = new List<object>();
            var skipped = new List<object>();

            using var tx = new Transaction(doc, "Add Family Parameters");
            tx.Start();

            EnsureCurrentType(familyManager);

            foreach (var definition in Parameters)
            {
                if (string.IsNullOrWhiteSpace(definition.Name))
                    continue;

                var existing = FindParameter(familyManager, definition.Name);
                if (existing != null)
                {
                    skipped.Add(new { name = definition.Name, reason = "already_exists" });
                    continue;
                }

                var parameter = FamilyEditorUtils.AddParameter(familyManager, definition);
                if (!string.IsNullOrWhiteSpace(definition.Formula))
                    familyManager.SetFormula(parameter, definition.Formula);
                else
                    FamilyEditorUtils.ApplyDefaultValue(familyManager, parameter, definition);

                created.Add(new
                {
                    name = parameter.Definition?.Name ?? definition.Name,
                    is_instance = parameter.IsInstance,
                    storage_type = parameter.StorageType.ToString(),
                });
            }

            tx.Commit();

            ResultInfo = new
            {
                created_count = created.Count,
                skipped_count = skipped.Count,
                created,
                skipped,
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public string GetName() => "Add Family Parameters";

    private static void EnsureCurrentType(FamilyManager familyManager)
    {
        if (familyManager.CurrentType != null)
            return;

        familyManager.NewType("Type 1");
    }

    private static FamilyParameter FindParameter(FamilyManager familyManager, string name)
    {
        foreach (FamilyParameter parameter in familyManager.Parameters)
        {
            if (string.Equals(parameter.Definition?.Name, name, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }

        return null;
    }
}
