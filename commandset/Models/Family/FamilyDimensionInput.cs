using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Family;

public class FamilyDimensionInput
{
    [JsonProperty("referencePlaneNames")]
    public List<string> ReferencePlaneNames { get; set; } = new();

    [JsonProperty("line")]
    public JZLine Line { get; set; } = new(new JZPoint(0, 0, 0), new JZPoint(1000, 0, 0));

    [JsonProperty("labelParameterName")]
    public string LabelParameterName { get; set; } = string.Empty;
}
