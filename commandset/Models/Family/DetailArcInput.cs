using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Family;

public class DetailArcInput
{
    [JsonProperty("center")]
    public JZPoint Center { get; set; } = new(0, 0, 0);

    [JsonProperty("radius")]
    public double Radius { get; set; } = 1000;

    [JsonProperty("startAngle")]
    public double StartAngle { get; set; }

    [JsonProperty("endAngle")]
    public double EndAngle { get; set; } = 90;

    [JsonProperty("lineStyle")]
    public string LineStyle { get; set; } = string.Empty;
}
