using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Family;

public class SymbolicLineInput
{
    [JsonProperty("line")]
    public JZLine Line { get; set; } = new(new JZPoint(0, 0, 0), new JZPoint(1000, 0, 0));

    [JsonProperty("lineStyle")]
    public string LineStyle { get; set; } = string.Empty;
}
