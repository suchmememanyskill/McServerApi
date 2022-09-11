using McServerApi.Model;
using McServerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace McServerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class Versions : ControllerBase
{
    private Storage _storage;
    public List<ServerTemplate> Servers => _storage.Servers;
    public CurrentConfiguration Config => _storage.CurrentConfiguration;

    public Versions(Storage storage)
    {
        _storage = storage;
    }
    
    [HttpGet]
    public IEnumerable<ServerTemplate> Get()
    {
        return Servers;
    }

    [HttpPost]
    public void Set(VersionPost data)
    {
        ServerTemplate? find = Servers.Find(x => x.Version == data.Version);

        if (find == null)
        {
            Response.StatusCode = 404;
            return;
        }

        Config.ServerVersion = find.Version;
        _storage.Save();
    }
}