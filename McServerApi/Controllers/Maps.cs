using System.IO.Compression;
using McServerApi.Model;
using McServerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace McServerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class Maps : ControllerBase
{
    private Storage _storage;
    private static string WORKDIR = "__mc_maps";
    private static string DELDIR = "__del_mc_maps";
    public List<MapTemplate> MapTemplates => _storage.Maps;
    public CurrentConfiguration Configuration => _storage.CurrentConfiguration;
    
    public Maps(Storage storage)
    {
        _storage = storage;
    }
    
    [HttpGet]
    public IEnumerable<MapTemplate> Get()
    {
        return MapTemplates;
    }

    [HttpPost]
    public void Set(MapSetPost data)
    {
        if (MapTemplates.All(x => x.Name != data.MapName) && data.MapName != "")
        {
            Response.StatusCode = 404;
            return;
        }

        if (data.MapName != "")
        {
            MapTemplate t = MapTemplates.Find(x => x.Name == data.MapName)!;
            if (_storage.Servers.Any(x => x.Version == t.MinecraftVersion))
                Configuration.ServerVersion = t.MinecraftVersion;
        }

        Configuration.MapName = data.MapName;
        _storage.Save();
    }

    [HttpPost("new")]
    public void New(MapsNewPost post)
    {
        MapTemplate template = CreateTemplate(post.Name, post.MinecraftVersion);
        Directory.CreateDirectory(template.Path);
        MapTemplates.Add(template);
        _storage.Save();
    }

    [HttpDelete("{map_name}")]
    public void Delete(string map_name)
    {
        MapTemplate? template = MapTemplates.Find(x => x.Name == map_name);

        if (template == null)
        {
            Response.StatusCode = 404;
            return;
        }

        Directory.CreateDirectory(DELDIR);
        
        string oldPath = template.Path;
        string newPath = Path.Join(DELDIR, $"{Path.GetFileName(template.Path)}_{Path.GetRandomFileName()}");
        
        Directory.Move(oldPath, newPath);
        MapTemplates.Remove(template);
        _storage.Save();
    }

    [HttpPost("{map_name}")]
    [DisableRequestSizeLimit]
    public void Create(string map_name, IFormFile file, string suggested_mc_version = "unk")
    {
        MapTemplate template = CreateTemplate(map_name, suggested_mc_version);
        
        if (file.Length > 0x10000000)
            throw new Exception("File size is over 256mb");
        
        if (!file.FileName.EndsWith(".zip"))
            throw new Exception("File is not a zip file");

        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        string zipPath = Path.Join(tempDirectory, Path.GetRandomFileName());
        Console.WriteLine(zipPath);
        using (var fs = new FileStream(zipPath, FileMode.Create))
        {
            file.CopyTo(fs);
        }

        ZipFile.ExtractToDirectory(zipPath, tempDirectory);

        if (!Directory.Exists(Path.Join(tempDirectory, "world")))
        {
            Response.StatusCode = 400;
        }
        else
        {
            Utils.CopyDirectory(Path.Join(tempDirectory, "world"), Path.Join(WORKDIR, map_name), true);
            MapTemplates.Add(template);
            _storage.Save();
        }
        
        Directory.Delete(tempDirectory, true);
    }

    private MapTemplate CreateTemplate(string name, string version)
    {
        if (name == null || version == null)
            throw new Exception("Parameters are null");
        
        if (Path.GetInvalidFileNameChars().Any(name.Contains))
            throw new Exception("Invalid map name");

        if (MapTemplates.Any(x => x.Name == name))
            throw new Exception("Map name already exists");
        
        if (version != "unk")
        {
            if (_storage.Servers.All(x => x.Version != version))
                throw new Exception("Invalid suggested mc version");
        }

        return new()
        {
            Name = name,
            MinecraftVersion = version,
            Path = Path.Join(WORKDIR, name)
        };
    }
}