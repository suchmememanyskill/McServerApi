using System.Text.Json.Serialization;
using McServerApi.Services;

namespace McServerApi.Model;

public class CurrentConfiguration
{
    public string MapName { get; set; } = "My Epic Map";
    public string ServerVersion { get; set; } = "1.0.0";
}