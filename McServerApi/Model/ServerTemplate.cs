using System.Text.Json.Serialization;

namespace McServerApi.Model;

public class ServerTemplate
{
    public string Version { get; set; } = "1.0.0";
    public string JavaVersion { get; set; } = "8";
    public string Url { get; set; }
    [JsonIgnore]
    public string AbsoluteServerPath { get; set; } = "";
}