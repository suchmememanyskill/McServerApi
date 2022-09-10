using McServerApi.Services;
using Newtonsoft.Json;

namespace McServerApi.Model;

public class CurrentConfiguration
{
    public string MapName { get; set; } = "My Epic Map";
    public string ServerVersion { get; set; } = "1.0.0";
    [JsonIgnore]
    public Server? Server { get; set; }

    [JsonIgnore] 
    public string TextStatus => Server?.Status.ToString() ?? "";
}