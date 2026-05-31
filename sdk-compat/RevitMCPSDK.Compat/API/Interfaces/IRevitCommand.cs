using Newtonsoft.Json.Linq;

namespace RevitMCPSDK.API.Interfaces;

public interface IRevitCommand
{
    string CommandName { get; }
    object Execute(JObject parameters, string requestId);
}
