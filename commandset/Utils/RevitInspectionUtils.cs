using Autodesk.Revit.DB;
using System.Collections;

namespace RevitMCPCommandSet.Utils;

internal static class RevitInspectionUtils
{
    public static long IdValue(ElementId id)
    {
#if REVIT2024_OR_GREATER
        return id?.Value ?? -1;
#else
        return id?.IntegerValue ?? -1;
#endif
    }

    public static string SafeName(Element element)
    {
        try { return element?.Name ?? "<unnamed>"; }
        catch { return "<unnamed>"; }
    }

    public static string LevelName(Document doc, ElementId id)
    {
        if (id == null || id == ElementId.InvalidElementId) return "<no-level>";
        var element = doc.GetElement(id);
        return element == null ? "<no-level>" : SafeName(element);
    }

    public static ElementId TryGetLevelId(Element element)
    {
        try { return element.LevelId; }
        catch { return ElementId.InvalidElementId; }
    }

    public static double ToMillimeters(double internalUnits)
    {
        return UnitUtils.ConvertFromInternalUnits(internalUnits, UnitTypeId.Millimeters);
    }

    public static double ToSquareMeters(double internalUnits)
    {
        return UnitUtils.ConvertFromInternalUnits(internalUnits, UnitTypeId.SquareMeters);
    }

    public static double ToCubicMeters(double internalUnits)
    {
        return UnitUtils.ConvertFromInternalUnits(internalUnits, UnitTypeId.CubicMeters);
    }

    public static IList<double> ToMillimeterPoint(XYZ xyz)
    {
        return new List<double> { ToMillimeters(xyz.X), ToMillimeters(xyz.Y), ToMillimeters(xyz.Z) };
    }

    public static Dictionary<string, object> DescribeLocation(Location location)
    {
        if (location is LocationPoint point)
        {
            return new Dictionary<string, object>
            {
                ["kind"] = "point",
                ["point_xyz_mm"] = ToMillimeterPoint(point.Point),
                ["rotation_degrees"] = point.Rotation * 180.0 / Math.PI,
            };
        }

        if (location is LocationCurve curve)
        {
            return new Dictionary<string, object>
            {
                ["kind"] = "curve",
                ["start_xyz_mm"] = ToMillimeterPoint(curve.Curve.GetEndPoint(0)),
                ["end_xyz_mm"] = ToMillimeterPoint(curve.Curve.GetEndPoint(1)),
                ["length_mm"] = ToMillimeters(curve.Curve.Length),
            };
        }

        return null;
    }

    public static Dictionary<string, object> DescribeBoundingBox(BoundingBoxXYZ boundingBox)
    {
        if (boundingBox == null) return null;
        return new Dictionary<string, object>
        {
            ["min_xyz_mm"] = ToMillimeterPoint(boundingBox.Min),
            ["max_xyz_mm"] = ToMillimeterPoint(boundingBox.Max),
        };
    }

    public static Parameter FindParameter(Element element, string parameterName)
    {
        var parameter = element.LookupParameter(parameterName);
        if (parameter != null) return parameter;

        var target = (parameterName ?? string.Empty).Trim();
        foreach (Parameter candidate in element.Parameters)
        {
            if (string.Equals(candidate?.Definition?.Name, target, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }
        return null;
    }

    public static Dictionary<string, object> SerializeParameter(Parameter parameter)
    {
        var dataType = parameter.Definition?.GetDataType();
        return new Dictionary<string, object>
        {
            ["name"] = parameter.Definition?.Name ?? string.Empty,
            ["storage_type"] = parameter.StorageType.ToString(),
            ["is_read_only"] = parameter.IsReadOnly,
            ["has_value"] = parameter.HasValue,
            ["value"] = GetParameterValue(parameter),
            ["display_value"] = parameter.AsValueString(),
            ["data_type"] = dataType?.TypeId ?? string.Empty,
        };
    }

    public static object GetParameterValue(Parameter parameter)
    {
        switch (parameter.StorageType)
        {
            case StorageType.String:
                return parameter.AsString();
            case StorageType.Integer:
                return parameter.AsInteger();
            case StorageType.Double:
                return parameter.AsDouble();
            case StorageType.ElementId:
                return IdValue(parameter.AsElementId());
            default:
                return null;
        }
    }

    public static bool TrySetParameterValue(Parameter parameter, object rawValue, string valueUnit, out string error)
    {
        error = null;
        try
        {
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    parameter.Set(rawValue == null ? string.Empty : Convert.ToString(rawValue));
                    return true;
                case StorageType.Integer:
                    if (rawValue is bool boolValue)
                    {
                        parameter.Set(boolValue ? 1 : 0);
                        return true;
                    }
                    parameter.Set(Convert.ToInt32(rawValue));
                    return true;
                case StorageType.Double:
                    parameter.Set(ConvertToParameterDouble(parameter, rawValue, valueUnit));
                    return true;
                case StorageType.ElementId:
                    parameter.Set(new ElementId(Convert.ToInt64(rawValue)));
                    return true;
                default:
                    error = "unsupported_parameter_storage_type";
                    return false;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static double ConvertToParameterDouble(Parameter parameter, object rawValue, string valueUnit)
    {
        var numericValue = Convert.ToDouble(rawValue);
        var dataType = parameter.Definition?.GetDataType();
        if (dataType == null || !UnitUtils.IsMeasurableSpec(dataType)) return numericValue;

        var requestedUnit = (valueUnit ?? string.Empty).Trim().ToLowerInvariant();
        ForgeTypeId unitTypeId = null;

        if (requestedUnit == "internal" || requestedUnit == "internal_units") return numericValue;
        if (dataType == SpecTypeId.Length) unitTypeId = UnitTypeId.Millimeters;
        else if (dataType == SpecTypeId.Area) unitTypeId = UnitTypeId.SquareMeters;
        else if (dataType == SpecTypeId.Volume) unitTypeId = UnitTypeId.CubicMeters;
        else if (dataType == SpecTypeId.Angle) unitTypeId = UnitTypeId.Degrees;

        if (requestedUnit == "mm") unitTypeId = UnitTypeId.Millimeters;
        if (requestedUnit == "m") unitTypeId = UnitTypeId.Meters;
        if (requestedUnit == "m2" || requestedUnit == "sqm") unitTypeId = UnitTypeId.SquareMeters;
        if (requestedUnit == "m3") unitTypeId = UnitTypeId.CubicMeters;
        if (requestedUnit == "deg" || requestedUnit == "degrees") unitTypeId = UnitTypeId.Degrees;

        return unitTypeId == null ? numericValue : UnitUtils.ConvertToInternalUnits(numericValue, unitTypeId);
    }
}
