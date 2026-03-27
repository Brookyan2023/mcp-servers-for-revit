using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Family;

namespace RevitMCPCommandSet.Utils;

internal static class FamilyEditorUtils
{
    public static Document RequireActiveDocument(UIApplication app)
    {
        var uiDoc = app.ActiveUIDocument ?? throw new InvalidOperationException("No active Revit document.");
        return uiDoc.Document ?? throw new InvalidOperationException("No active Revit document.");
    }

    public static Document RequireActiveFamilyDocument(UIApplication app)
    {
        var doc = RequireActiveDocument(app);
        if (!doc.IsFamilyDocument)
            throw new InvalidOperationException("The active Revit document is not a family document.");
        return doc;
    }

    public static View RequireUsableView(Document doc)
    {
        var view = doc.ActiveView ?? throw new InvalidOperationException("No active view.");
        if (view.IsTemplate)
            throw new InvalidOperationException("The active view is a template and cannot host family editor geometry.");
        return view;
    }

    public static GraphicsStyle FindLineStyle(Document doc, string styleName)
    {
        if (string.IsNullOrWhiteSpace(styleName))
            return null;

        var lineCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
        if (lineCategory == null)
            return null;

        foreach (Category subCategory in lineCategory.SubCategories)
        {
            if (string.Equals(subCategory.Name, styleName, StringComparison.OrdinalIgnoreCase))
                return subCategory.GetGraphicsStyle(GraphicsStyleType.Projection);
        }

        return null;
    }

    public static FilledRegionType FindFilledRegionType(Document doc, string fillPatternName)
    {
        var types = new FilteredElementCollector(doc)
            .OfClass(typeof(FilledRegionType))
            .Cast<FilledRegionType>()
            .ToList();

        if (types.Count == 0)
            return null;

        if (string.IsNullOrWhiteSpace(fillPatternName))
            return types.FirstOrDefault();

        return types.FirstOrDefault(t =>
                   string.Equals(t.Name, fillPatternName, StringComparison.OrdinalIgnoreCase))
               ?? types.FirstOrDefault(t => t.Name.IndexOf(fillPatternName, StringComparison.OrdinalIgnoreCase) >= 0)
               ?? types.FirstOrDefault();
    }

    public static XYZ ResolveViewRightDirection(View view)
    {
        try
        {
            if (view.RightDirection != null && view.RightDirection.GetLength() > 1e-9)
                return view.RightDirection.Normalize();
        }
        catch
        {
        }

        return XYZ.BasisX;
    }

    public static XYZ ResolveViewUpDirection(View view)
    {
        try
        {
            if (view.UpDirection != null && view.UpDirection.GetLength() > 1e-9)
                return view.UpDirection.Normalize();
        }
        catch
        {
        }

        return XYZ.BasisY;
    }

    public static ReferencePlane FindReferencePlane(Document doc, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return new FilteredElementCollector(doc)
            .OfClass(typeof(ReferencePlane))
            .Cast<ReferencePlane>()
            .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static FamilyParameter FindFamilyParameter(FamilyManager familyManager, string name)
    {
        if (familyManager == null || string.IsNullOrWhiteSpace(name))
            return null;

        foreach (FamilyParameter parameter in familyManager.Parameters)
        {
            if (string.Equals(parameter.Definition?.Name, name, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }

        return null;
    }

    public static TextNoteType FindTextNoteType(Document doc, int typeId)
    {
        if (typeId > 0)
        {
            var type = doc.GetElement(new ElementId(typeId)) as TextNoteType;
            if (type != null)
                return type;
        }

        return new FilteredElementCollector(doc)
            .OfClass(typeof(TextNoteType))
            .Cast<TextNoteType>()
            .FirstOrDefault();
    }

    public static SketchPlane CreateViewSketchPlane(Document doc, View view, XYZ origin)
    {
        var plane = Plane.CreateByNormalAndOrigin(view.ViewDirection, origin);
        return SketchPlane.Create(doc, plane);
    }

    public static object ConvertDefaultValue(FamilyParameterDefinition definition)
    {
        if (definition.DefaultValue == null || definition.DefaultValue.Type == JTokenType.Null)
            return null;

        return definition.DefaultValue.Type switch
        {
            JTokenType.Integer => definition.DefaultValue.Value<long>(),
            JTokenType.Float => definition.DefaultValue.Value<double>(),
            JTokenType.Boolean => definition.DefaultValue.Value<bool>(),
            _ => definition.DefaultValue.Value<string>(),
        };
    }

#if REVIT2022_OR_GREATER
    public static ForgeTypeId ResolveGroupTypeId(string group)
    {
        return Normalize(group) switch
        {
            "constraints" => GroupTypeId.Constraints,
            "construction" => GroupTypeId.Construction,
            "dimensions" => GroupTypeId.Geometry,
            "geometry" => GroupTypeId.Geometry,
            "graphics" => GroupTypeId.Graphics,
            "identitydata" => GroupTypeId.IdentityData,
            "materialsandfinishes" => GroupTypeId.Materials,
            "materials" => GroupTypeId.Materials,
            "text" => GroupTypeId.Text,
            _ => GroupTypeId.Data,
        };
    }

    public static ForgeTypeId ResolveSpecTypeId(string dataType)
    {
        return Normalize(dataType) switch
        {
            "angle" => SpecTypeId.Angle,
            "area" => SpecTypeId.Area,
            "integer" => SpecTypeId.Int.Integer,
            "length" => SpecTypeId.Length,
            "material" => SpecTypeId.Reference.Material,
            "number" => SpecTypeId.Number,
            "yesno" => SpecTypeId.Boolean.YesNo,
            _ => SpecTypeId.String.Text,
        };
    }

    public static FamilyParameter AddParameter(FamilyManager familyManager, FamilyParameterDefinition definition)
    {
        return familyManager.AddParameter(
            definition.Name,
            ResolveGroupTypeId(definition.Group),
            ResolveSpecTypeId(definition.DataType),
            definition.IsInstance);
    }
#else
    public static BuiltInParameterGroup ResolveParameterGroup(string group)
    {
        return Normalize(group) switch
        {
            "constraints" => BuiltInParameterGroup.PG_CONSTRAINTS,
            "construction" => BuiltInParameterGroup.PG_CONSTRUCTION,
            "dimensions" => BuiltInParameterGroup.PG_GEOMETRY,
            "geometry" => BuiltInParameterGroup.PG_GEOMETRY,
            "graphics" => BuiltInParameterGroup.PG_GRAPHICS,
            "identitydata" => BuiltInParameterGroup.PG_IDENTITY_DATA,
            "materialsandfinishes" => BuiltInParameterGroup.PG_MATERIALS,
            "materials" => BuiltInParameterGroup.PG_MATERIALS,
            "text" => BuiltInParameterGroup.PG_TEXT,
            _ => BuiltInParameterGroup.PG_DATA,
        };
    }

    public static ParameterType ResolveParameterType(string dataType)
    {
        return Normalize(dataType) switch
        {
            "angle" => ParameterType.Angle,
            "area" => ParameterType.Area,
            "integer" => ParameterType.Integer,
            "length" => ParameterType.Length,
            "material" => ParameterType.Material,
            "number" => ParameterType.Number,
            "yesno" => ParameterType.YesNo,
            _ => ParameterType.Text,
        };
    }

    public static FamilyParameter AddParameter(FamilyManager familyManager, FamilyParameterDefinition definition)
    {
        return familyManager.AddParameter(
            definition.Name,
            ResolveParameterGroup(definition.Group),
            ResolveParameterType(definition.DataType),
            definition.IsInstance);
    }
#endif

    public static void ApplyDefaultValue(FamilyManager familyManager, FamilyParameter parameter, FamilyParameterDefinition definition)
    {
        var value = ConvertDefaultValue(definition);
        if (value == null)
            return;

        switch (parameter.StorageType)
        {
            case StorageType.String:
                familyManager.Set(parameter, Convert.ToString(value));
                break;
            case StorageType.Integer:
                if (value is bool boolValue)
                    familyManager.Set(parameter, boolValue ? 1 : 0);
                else
                    familyManager.Set(parameter, Convert.ToInt32(value));
                break;
            case StorageType.Double:
                familyManager.Set(parameter, ConvertToInternalDouble(definition, Convert.ToDouble(value)));
                break;
            case StorageType.ElementId:
                familyManager.Set(parameter, new ElementId(Convert.ToInt64(value)));
                break;
        }
    }

    private static double ConvertToInternalDouble(FamilyParameterDefinition definition, double value)
    {
        return Normalize(definition.DataType) switch
        {
            "angle" => UnitUtils.ConvertToInternalUnits(value, UnitTypeId.Degrees),
            "area" => UnitUtils.ConvertToInternalUnits(value, UnitTypeId.SquareMeters),
            "length" => UnitUtils.ConvertToInternalUnits(value, UnitTypeId.Millimeters),
            _ => value,
        };
    }

    public static string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .ToLowerInvariant();
    }
}
