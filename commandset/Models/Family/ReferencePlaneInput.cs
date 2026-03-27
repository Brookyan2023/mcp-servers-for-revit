using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.Family;

public class ReferencePlaneInput
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("bubbleEnd")]
    public JZPoint BubbleEnd { get; set; } = new(0, 0, 0);

    [JsonProperty("freeEnd")]
    public JZPoint FreeEnd { get; set; } = new(1000, 0, 0);

    [JsonProperty("cutVector")]
    public JZPoint CutVector { get; set; } = new(0, 0, 1000);
}
