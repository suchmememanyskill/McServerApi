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

        Configuration.MapName = data.MapName;
        _storage.Save();
    }

    [HttpPost("{map_name}")]
    [DisableRequestSizeLimit]
    public void Create(string map_name, IFormFile file, string suggested_mc_version = "unk")
    {
        if (file.Length > 0x10000000)
            throw new Exception("File size is over 256mb");
        
        if (!file.FileName.EndsWith(".zip"))
            throw new Exception("File is not a zip file");

        if (Path.GetInvalidFileNameChars().Any(map_name.Contains))
            throw new Exception("Invalid map name");
        
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
            MapTemplates.Add(new()
            {
                Name = map_name,
                MinecraftVersion = suggested_mc_version,
                Path = Path.Join(WORKDIR, map_name)
            });
            _storage.Save();
        }
        
        Directory.Delete(tempDirectory, true);
    }
}