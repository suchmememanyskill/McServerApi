using System.Diagnostics;
using System.Net;
using LauncherGamePlugin;
using McServerApi.Model;

namespace McServerApi.Services;

public enum ServerStatus
{
    Stopped,
    Initialising,
    Started,
    Ready,
    Stopping,
    Dead,
}

public class Server
{
    private Terminal _terminal = new();
    private Storage _storage;
    private static string WORKDIR = "__mc_server";
    private static string TEMPLATEDIR = "__mc_server_template";
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

    public Server(Storage storage)
    {
        _storage = storage;
        _terminal.OnNewLine += (terminal, s) =>
        {
            if (s.Contains("[Server thread/INFO]: Done ("))
                Status = ServerStatus.Ready;
        };
    }

    public async void Start()
    {
        if (!(Status is ServerStatus.Stopped or ServerStatus.Dead))
            return;
        
        Status = ServerStatus.Initialising;
        if (Directory.Exists(WORKDIR))
            Directory.Delete(WORKDIR, true);
        
        ServerTemplate? mcServerJar = _storage.Servers.Find(x => x.Version == _storage.CurrentConfiguration.ServerVersion);
        MapTemplate? mcServerMap = _storage.Maps.Find(x => x.Name == _storage.CurrentConfiguration.MapName);
        JavaTemplate? mcServerJava = _storage.Javas.Find(x => x.Version == (mcServerJar?.JavaVersion ?? "_"));

        if (mcServerJar == null || mcServerJava == null)
        {
            Console.WriteLine("Invalid configuration");
            Status = ServerStatus.Dead;
            return;
        }

        if (!string.IsNullOrWhiteSpace(mcServerJar.AbsoluteServerPath))
        {
            Launch(mcServerJar.AbsoluteServerPath, mcServerJava);
            return;
        }
        
        Utils.CopyDirectory(TEMPLATEDIR, WORKDIR, true);

        using (HttpClient client = new())
        {
            var response = await client.GetAsync(mcServerJar.Url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Invalid server url");
                Status = ServerStatus.Dead;
                return;
            }

            await using (var fs = new FileStream(Path.Join(WORKDIR, "server.jar"), FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        if (mcServerMap == null)
        {
            Console.WriteLine("Map not found, not creating symlink");
        }
        else
        {
            if (!Directory.Exists(mcServerMap.Path))
            {
                Console.WriteLine("Invalid map");
                Status = ServerStatus.Dead;
                return;
            }

            var res = File.CreateSymbolicLink(Path.Join(WORKDIR, "world"), Path.GetFullPath(mcServerMap.Path));
        }
        
        Launch(WORKDIR, mcServerJava);
    }

    public async void Launch(string workingDir, JavaTemplate java)
    {
        _terminal.WorkingDirectory = workingDir;
        Status = ServerStatus.Started;
        bool result = await _terminal.Exec(java.Path, "-Xmx8G -Xms1G -jar server.jar nogui");

        if (!result)
            Status = ServerStatus.Dead;
        else
            Status = (_terminal.ExitCode == 0) ? ServerStatus.Stopped : ServerStatus.Dead;
    }

    public void Stop()
    {
        if (Status != ServerStatus.Ready)
            return;

        Status = ServerStatus.Stopping;
        _terminal.WriteToStdIn("stop");
    }
}