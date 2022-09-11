using Newtonsoft.Json;

namespace McServerApi.Model;

public class MapTemplate
{
    public string Name { get; set; } = "My Epic Map";
    [System.Text.Json.Serialization.JsonIgnore]
    public string Path { get; set; } = "~/maps/my_world";
    public string MinecraftVersion { get; set; } = "unk";
    public bool ReadOnly { get; set; }
    [JsonIgnore] 
    public bool HasResourcePack => File.Exists(System.IO.Path.Join(Path, "resources.zip"));
}