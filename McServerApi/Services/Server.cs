using System.Net;
using LauncherGamePlugin;
using McServerApi.Model;

namespace McServerApi.Services;

public enum ServerStatus
{
    Stopped,
    Initialising,
    Started,
    Stopping,
    Dead,
}

public class Server
{
    private Terminal _terminal = new();
    private Storage _storage;
    private static string WORKDIR = "__mc_server";
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;

    public Server(Storage storage) => _storage = storage;

    public async void Start()
    {
        Status = ServerStatus.Initialising;
        if (Directory.Exists(WORKDIR))
            Directory.Delete(WORKDIR, true);

        Directory.CreateDirectory(WORKDIR);

        ServerTemplate? mcServerJar =
            _storage.Servers.Find(x => x.Version == _storage.CurrentConfiguration.ServerVersion);

        MapTemplate? mcServerMap = _storage.Maps.Find(x => x.Name == _storage.CurrentConfiguration.MapName);

        JavaTemplate? mcServerJava = _storage.Javas.Find(x => x.Version == (mcServerJar?.JavaVersion ?? "_"));

        if (mcServerJar == null || mcServerMap == null || mcServerJava == null)
        {
            Console.WriteLine("Invalid configuration");
            Status = ServerStatus.Dead;
            return;
        }

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

        if (!mcServerMap.Path.EndsWith("world") && Directory.Exists(mcServerMap.Path))
        {
            Console.WriteLine("Invalid map");
            Status = ServerStatus.Dead;
            return;
        }

        File.CreateSymbolicLink(Path.Join(WORKDIR, "world"), mcServerMap.Path);
    }
}