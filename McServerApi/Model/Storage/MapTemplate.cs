using Newtonsoft.Json;

namespace McServerApi.Model;

public class MapTemplate
{
    public string Name { get; init; } = "My Epic Map";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Path { get; init; } = "~/maps/my_world";
    public string MinecraftVersion { get; init; } = "unk";
    public bool ReadOnly { get; init; }
    [JsonIgnore] 
    public bool HasResourcePack => File.Exists(System.IO.Path.Join(Path, "resources.zip"));
}