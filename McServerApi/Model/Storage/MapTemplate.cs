using System.Text.Json.Serialization;

namespace McServerApi.Model;

public class MapTemplate
{
    public string Name { get; set; } = "My Epic Map";
    [JsonIgnore]
    public string Path { get; set; } = "~/maps/my_world";
    public string MinecraftVersion { get; set; } = "unk";
    public bool ReadOnly { get; set; }
}