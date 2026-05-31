using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCPSDK.API.Models.JsonRPC;

public class JsonRPCRequest
{
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public JToken Params { get; set; }

    public bool IsValid()
    {
        return string.Equals(JsonRpc, "2.0", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(Method);
    }

    public JObject GetParamsObject()
    {
        if (Params == null || Params.Type == JTokenType.Null) return new JObject();
        if (Params is JObject obj) return obj;
        return new JObject { ["value"] = Params };
    }
}
