using McServerApi.Model;
using McServerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace McServerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class Config : ControllerBase
{
    private Storage _storage;
    private Server _server;
    public CurrentConfiguration CurConfig => _storage.CurrentConfiguration;
    
    public Config(Storage storage, Server server)
    {
        _storage = storage;
        _server = server;
    }

    [HttpGet]
    public CurrentConfiguration Get()
    {
        CurConfig.Server = _server;
        return CurConfig;
    }

    [HttpPost("state")]
    public void State(bool state)
    {
        
    }
}