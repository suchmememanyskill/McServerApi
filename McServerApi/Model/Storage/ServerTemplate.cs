using Newtonsoft.Json;

namespace McServerApi.Model;

public class ServerTemplate
{
    public string Version { get; set; } = "1.0.0";
    public string JavaVersion { get; set; } = "8";
    [JsonIgnore]
    public bool UsesMaps => !string.IsNullOrWhiteSpace(Url);
    [System.Text.Json.Serialization.JsonIgnore]
    public string Url { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string AbsoluteServerPath { get; set; } = "";
}