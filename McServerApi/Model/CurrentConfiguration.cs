using System.Text.Json.Serialization;
using McServerApi.Services;

namespace McServerApi.Model;

public class CurrentConfiguration
{
    public string MapName { get; set; } = "My Epic Map";
    public string ServerVersion { get; set; } = "1.0.0";
    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public Server? Server { get; set; }

    [Newtonsoft.Json.JsonIgnore] 
    public string TextStatus => Server?.Status.ToString() ?? "";

    [Newtonsoft.Json.JsonIgnore] 
    public List<string> OnlinePlayers => Server?.OnlinePlayers ?? new();
}