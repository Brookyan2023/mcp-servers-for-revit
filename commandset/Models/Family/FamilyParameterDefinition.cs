using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCPCommandSet.Models.Family;

public class FamilyParameterDefinition
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("group")]
    public string Group { get; set; } = "Data";

    [JsonProperty("dataType")]
    public string DataType { get; set; } = "Text";

    [JsonProperty("isInstance")]
    public bool IsInstance { get; set; }

    [JsonProperty("defaultValue")]
    public JToken DefaultValue { get; set; }

    [JsonProperty("formula")]
    public string Formula { get; set; } = string.Empty;
}
