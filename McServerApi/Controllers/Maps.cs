using System.IO.Compression;
using System.Web;
using McServerApi.Model;
using McServerApi.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace McServerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class Maps : ControllerBase
{
    private Storage _storage;
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

    [HttpGet("resources/{map_name}")]
    public ActionResult GetResources(string map_name)
    {
        var map = MapTemplates.Find(x => x.Name == map_name);
        if (map == null)
        {
            map_name = HttpUtility.UrlDecode(map_name);
            map = MapTemplates.Find(x => x.Name == map_name);
            if (map == null)
                return NotFound();
        }
            
        
        if (!System.IO.File.Exists(Path.Join(map.Path, "resources.zip")))
            return NotFound();

        return File(System.IO.File.ReadAllBytes(Path.Join(map.Path, "resources.zip")), "application/zip", $"{map.Name}.zip");
    }

    [HttpPut]
    public string Set(MapsPost data)
    {
        if (MapTemplates.All(x => x.Name != data.MapName) && data.MapName != "")
        {
            Response.StatusCode = 404;
            return "Could not find map";
        }

        if (data.MapName != "")
        {
            MapTemplate t = MapTemplates.Find(x => x.Name == data.MapName)!;
            if (_storage.Servers.Any(x => x.Version == t.MinecraftVersion))
                Configuration.ServerVersion = t.MinecraftVersion;
        }

        Configuration.MapName = data.MapName;
        _storage.WriteConfiguration();
        return "OK";
    }

    [HttpPut("{map_name}/version")]
    public string ChangeVersion(string map_name, MapNameVersionPost data)
    {
        try
        {
            _storage.MapSetVersion(map_name, data.Version);
            return "OK";
        }
        catch (Exception e)
        {
            Response.StatusCode = 400;
            return e.Message;
        }
    }

    [HttpPost("new")]
    public string New(MapsNewPost post)
    {
        try
        {
            ValidateMapInput(post.Name, post.MinecraftVersion);
            Directory.CreateDirectory(Path.Join(Storage.MAPSDIR, post.Name));
            _storage.MapSetVersion(post.Name, post.MinecraftVersion);
            return "OK";
        }
        catch (Exception e)
        {
            Response.StatusCode = 400;
            return e.Message;
        }
    }

    [HttpDelete("{map_name}")]
    public string Delete(string map_name)
    {
        MapTemplate? template = MapTemplates.Find(x => x.Name == map_name);

        if (template == null)
        {
            Response.StatusCode = 404;
            return "Could not find map";
        }

        Directory.CreateDirectory(Storage.DELETEDMAPSDIR);
        
        string oldPath = template.Path;
        string newPath = Path.Join(Storage.DELETEDMAPSDIR, $"{Path.GetFileName(template.Path)}_{Path.GetRandomFileName()}");
        
        Directory.Move(oldPath, newPath);
        _storage.Reload();
        return "OK";
    }

    [HttpPost("{map_name}")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 0x10000000)]
    public string Create(string map_name, IFormFile file, string suggested_mc_version = "unk", bool read_only = false)
    {
        try
        { 
            CreateActual(map_name, file, suggested_mc_version, read_only);
            return "OK";
        }
        catch (Exception e)
        {
            Response.StatusCode = 400;
            return e.Message;
        }
    }

    private void CreateActual(string map_name, IFormFile file, string suggested_mc_version = "unk", bool read_only = false)
    {
        ValidateMapInput(map_name, suggested_mc_version);
        
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
            Directory.Delete(tempDirectory, true);
            throw new Exception("Zip does not contain a world folder");
        }

        Utils.CopyDirectory(Path.Join(tempDirectory, "world"), Path.Join(Storage.MAPSDIR, map_name), true);
        
        _storage.MapSetVersion(map_name, suggested_mc_version);
        _storage.MapSetReadOnly(map_name, read_only);
        Directory.Delete(tempDirectory, true);
    }

    private void ValidateMapInput(string name, string version)
    {
        if (name == null || version == null)
            throw new Exception("Parameters are null");
        
        if (Path.GetInvalidFileNameChars().Any(name.Contains))
            throw new Exception("Invalid map name");

        if (MapTemplates.Any(x => x.Name == name))
            throw new Exception("Map name already exists");
        
        if (version != "unk")
        {
            ServerTemplate? server = _storage.Servers.Find(x => x.Version == version);
            
            if (server == null)
                throw new Exception("Invalid suggested mc version");

            if (!server.UsesMaps)
                throw new Exception("Provided server does not use maps");
        }
    }
}