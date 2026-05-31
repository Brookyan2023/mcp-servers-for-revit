using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCPSDK.API.Models.JsonRPC;

public class JsonRPCError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public JToken Data { get; set; }
}

public class JsonRPCErrorResponse
{
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("error")]
    public JsonRPCError Error { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
