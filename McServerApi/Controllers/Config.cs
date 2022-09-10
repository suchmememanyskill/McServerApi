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
    public void State(ConfigStatusPost data)
    {
        if (data.Status)
        {
            if (!(_server.Status is ServerStatus.Stopped or ServerStatus.Dead))
            {
                Response.StatusCode = 409;
                return;
            }
            _server.Start();
        }
        else
        {
            if (_server.Status != ServerStatus.Ready)
            {
                Response.StatusCode = 409;
                return;
            }
            
            _server.Stop();
        }
            
    }
}