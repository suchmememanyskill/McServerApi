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
            if (s.Contains("[Server thread/INFO]: Done (") || s.Contains("Can't keep up! Did the system time change, or is the server overloaded?") || s.Contains("[minecraft/DedicatedServer]: Done ("))
                Status = ServerStatus.Ready;
        };
    }

    public async void Start()
    {
        if (!(Status is ServerStatus.Stopped or ServerStatus.Dead))
            return;
        
        Status = ServerStatus.Initialising;
        
        ServerTemplate? mcServerJar = _storage.Servers.Find(x => x.Version == _storage.CurrentConfiguration.ServerVersion);
        MapTemplate? mcServerMap = _storage.Maps.Find(x => x.Name == _storage.CurrentConfiguration.MapName);
        JavaTemplate? mcServerJava = _storage.Javas.Find(x => x.Version == (mcServerJar?.JavaVersion ?? "_"));

        if (mcServerJar == null || mcServerJava == null)
        {
            Log("Invalid configuration");
            Status = ServerStatus.Dead;
            return;
        }

        if (!string.IsNullOrWhiteSpace(mcServerJar.AbsoluteServerPath))
        {
            Launch(mcServerJar.AbsoluteServerPath, mcServerJava);
            return;
        }
        
        if (Directory.Exists(WORKDIR))
        {
            Log("Deleting old directory");
            Directory.Delete(WORKDIR, true);
        }
        
        Log("Creating new directory");
        Utils.CopyDirectory(TEMPLATEDIR, WORKDIR, true);
        
        Log("Downloading new .jar");
        using (HttpClient client = new())
        {
            var response = await client.GetAsync(mcServerJar.Url);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Log("Invalid server url");
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
            Log("Map not found, not creating symlink");
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
        Log("Starting server");
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

    public void Log(string msg) => Console.WriteLine($"[Server] {msg}");
}