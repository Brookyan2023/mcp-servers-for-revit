using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Family;

public class FilledRegionInput
{
    [JsonProperty("boundary")]
    public List<JZPoint> Boundary { get; set; } = new();

    [JsonProperty("fillPatternName")]
    public string FillPatternName { get; set; } = string.Empty;
}
