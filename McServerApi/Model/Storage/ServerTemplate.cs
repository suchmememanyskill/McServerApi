using Newtonsoft.Json;

namespace McServerApi.Model;

public class ServerTemplate
{
    public string Version { get; set; } = "1.0.0";
    public string JavaVersion { get; set; } = "latest";
    [JsonIgnore]
    public bool UsesMaps => string.IsNullOrWhiteSpace(AbsoluteServerPath) || (AbsoluteServerPath?.EndsWith(".sh") ?? false);

    [System.Text.Json.Serialization.JsonIgnore]
    public List<string> Mappings { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public List<string> ServerMappings { get; set; } = new();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> Downloadables { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public string AbsoluteServerPath { get; set; } = "";
}