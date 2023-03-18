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

    [HttpPut("state")]
    public string State(StatusStatePost data)
    {
        string url = $"{Request.Scheme}://{Request.Host.Value}";
        
        if (data.Status)
        {
            if (!(_server.Status is ServerStatus.Stopped or ServerStatus.Dead))
            {
                Response.StatusCode = 409;
                return "Server is already active";
            }
            _server.Start(url);
        }
        else
        {
            if (_server.Status != ServerStatus.Ready)
            {
                Response.StatusCode = 409;
                return "Server is not ready";
            }
            
            _server.Stop();
        }

        return "OK";
    }

    [HttpPost("command")]
    public string Command(CommandPost commandPost)
    {
        if (_server.Status != ServerStatus.Ready)
        {
            Response.StatusCode = 409;
            return "Server is not ready";
        }
        
        _server.RunCommand(commandPost.Command);
        return "OK";
    }
    
    [HttpPost("kill")]
    public string Kill()
    {
        if (_server.Status is ServerStatus.Stopped or ServerStatus.Dead)
        {
            Response.StatusCode = 409;
            return "Server is stopped";
        }
        
        _server.Kill();
        return "OK";
    }
}