using System.Diagnostics;
using McServerApi.Model;
using Newtonsoft.Json;

namespace McServerApi.Services;

public class Storage
{
    private StorageDataDefaults _dataDefaults = new();
    public List<ServerTemplate> Servers { get; private set; }
    public List<MapTemplate> Maps { get; private set; }
    public List<JavaTemplate> Javas => _dataDefaults.Javas;
    public CurrentConfiguration CurrentConfiguration { get; private set; }

    private static string FILENAME = "data.json";

    public static string DELETEDMAPSDIR = "__del_mc_maps";
    public static string JARCACHEDIR = "__jar_cache";
    public static string MAPSDIR = "__mc_maps";
    public static string TEMPLATEDIR = "__mc_server_template";
    public static string SERVERMAPPINGSDIR = "__mc_server_mappings";

    public Storage()
    {
        Reload();
    }

    private void LoadMaps()
    {
        Maps = Directory.EnumerateDirectories(MAPSDIR).ToList().Select(x => new MapTemplate()
            {
                MinecraftVersion = (File.Exists(Path.Join(x, "VERSION")))
                    ? File.ReadAllText(Path.Join(x, "VERSION")).Trim()
                    : "unk",
                Name = Path.GetFileName(x),
                Path = x,
                ReadOnly = File.Exists(Path.Join(x, "READONLY"))
            }
        ).ToList();
    }

    private void LoadConfiguration()
    {
        CurrentConfiguration = new CurrentConfiguration();
        string path = Path.Join(MAPSDIR, "current.json");
        if (File.Exists(path))
            CurrentConfiguration = JsonConvert.DeserializeObject<CurrentConfiguration>(File.ReadAllText(path))!;
    }
    
    private List<ServerTemplate> GetExtraServerTemplates()
    {
        List<ServerTemplate> templates = new();
        string path = Path.Join(MAPSDIR, "versions.json");
        if (File.Exists(path))
            templates = JsonConvert.DeserializeObject<List<ServerTemplate>>(File.ReadAllText(path))!;

        return templates;
    }

    public void WriteConfiguration()
    {
        File.WriteAllText(Path.Join(MAPSDIR, "current.json"), JsonConvert.SerializeObject(CurrentConfiguration));
    }

    public void Reload()
    {
        _dataDefaults = JsonConvert.DeserializeObject<StorageDataDefaults>(File.ReadAllText(FILENAME))!;
        Servers = _dataDefaults.Servers.Concat(GetExtraServerTemplates()).ToList();
        LoadConfiguration();
        LoadMaps();
    }

    public void MapSetVersion(string map, string version)
    {
        MapTemplate? mapTemplate = Maps.FirstOrDefault(x => x.Name == map);
        
        if (mapTemplate == null)
            Reload();
        
        mapTemplate = Maps.FirstOrDefault(x => x.Name == map);

        if (mapTemplate == null)
            throw new Exception($"Failed to find map '{map}'");

        if (version == "unk")
        {
            File.WriteAllText(Path.Join(mapTemplate.Path, "VERSION"), "unk");
            Reload();
            return;
        }

        ServerTemplate? server = Servers.FirstOrDefault(x => x.Version == version);

        if (server == null)
            throw new Exception($"Failed to find server with version '{version}'");

        if (!server.UsesMaps)
            throw new Exception("Specified version does not support maps");
        
        File.WriteAllText(Path.Join(mapTemplate.Path, "VERSION"), server.Version);
        Reload();

        if (CurrentConfiguration.MapName == mapTemplate.Name)
        {
            CurrentConfiguration.ServerVersion = server.Version;
            WriteConfiguration();
        }
    }
    
    public void MapSetReadOnly(string map, bool state)
    {
        MapTemplate? mapTemplate = Maps.FirstOrDefault(x => x.Name == map);
        
        if (mapTemplate == null)
            Reload();
        
        mapTemplate = Maps.FirstOrDefault(x => x.Name == map);

        if (mapTemplate == null)
            throw new Exception($"Failed to find map '{map}'");

        string path = Path.Join(mapTemplate.Path, "READONLY");
        if (state && !File.Exists(path))
            File.WriteAllText(path, "");
        else if (!state && File.Exists(path))
            File.Delete(path);
        
        Reload();
    }
}

public class StorageDataDefaults
{
    public List<ServerTemplate> Servers { get; } = new();
    public List<JavaTemplate> Javas { get; } = new();
}