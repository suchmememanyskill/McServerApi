using McServerApi.Model;
using McServerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace McServerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class Status : ControllerBase
{
    private Storage _storage;
    private Server _server;
    public CurrentConfiguration CurConfig => _storage.CurrentConfiguration;
    
    public Status(Storage storage, Server server)
    {
        _storage = storage;
        _server = server;
    }

    [HttpGet]
    public StatusGet Get()
    {
        return new(_storage, _server);
    }

    [HttpPost("state")]
    public void State(StatusStatePost data)
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