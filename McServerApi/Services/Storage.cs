using System.Diagnostics;
using McServerApi.Model;
using Newtonsoft.Json;

namespace McServerApi.Services;

public class Storage
{
    private StorageData _data = new();
    public List<ServerTemplate> Servers => _data.Servers;
    public List<MapTemplate> Maps => _data.Maps;
    public List<JavaTemplate> Javas => _data.Javas;
    public CurrentConfiguration CurrentConfiguration => _data.CurrentConfiguration;

    private static string FILENAME = "ServerData.json";
    
    public Storage()
    {
        if (File.Exists(FILENAME))
            _data = JsonConvert.DeserializeObject<StorageData>(File.ReadAllText(FILENAME))!;
    }

    public void Save() => File.WriteAllText(FILENAME, JsonConvert.SerializeObject(_data));
}

public class StorageData
{
    public List<ServerTemplate> Servers { get; set; } = new();
    public List<MapTemplate> Maps { get; set; } = new();
    public List<JavaTemplate> Javas { get; set; } = new();
    public CurrentConfiguration CurrentConfiguration { get; set; } = new();
}