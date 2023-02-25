using System.Diagnostics;
using System.Net;
using System.Web;
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
    private JarCache _cache;
    private AppConfiguration _config;
    private static string WORKDIR = "__mc_server";
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;
    public List<string> OnlinePlayers { get; private set; } = new();
    public event Action<ServerStatus> OnStatusChange;

    public Server(Storage storage, JarCache cache, AppConfiguration config)
    {
        _storage = storage;
        _cache = cache;
        _config = config;

        OnStatusChange += x =>
        {
            if (x == ServerStatus.Ready)
            {
                _terminal.OnNewLine -= MonitorStatusToReady;

                _terminal.OnNewLine -= MonitorPlayerList;
                _terminal.OnNewLine += MonitorPlayerList;
            }
            else
            {
                OnlinePlayers = new();
                _terminal.OnNewLine -= MonitorStatusToReady;
                _terminal.OnNewLine += MonitorStatusToReady;

                _terminal.OnNewLine -= MonitorPlayerList;
            }
        };
    }

    public async void Start(string baseUrl)
    {
        if (!(Status is ServerStatus.Stopped or ServerStatus.Dead))
            return;
        
        ChangeStatus(ServerStatus.Initialising);
        
        ServerTemplate? mcServerJar = _storage.Servers.Find(x => x.Version == _storage.CurrentConfiguration.ServerVersion);
        MapTemplate? mcServerMap = _storage.Maps.Find(x => x.Name == _storage.CurrentConfiguration.MapName);
        JavaTemplate? mcServerJava = _storage.Javas.Find(x => x.Version == (mcServerJar?.JavaVersion ?? "_"));

        if (mcServerJar == null || mcServerJava == null || (mcServerMap == null && _storage.CurrentConfiguration.MapName != ""))
        {
            Log("Invalid configuration");
            ChangeStatus(ServerStatus.Dead);
            return;
        }

        if (!string.IsNullOrWhiteSpace(mcServerJar.AbsoluteServerPath))
        {
            await Launch(mcServerJar.AbsoluteServerPath, mcServerJava);
            return;
        }

        string expectedMapMcVersion = mcServerMap?.MinecraftVersion ?? "unk";

        if (expectedMapMcVersion != "unk" && expectedMapMcVersion != mcServerJar.Version)
        {
            Log("Invalid map for server version");
            ChangeStatus(ServerStatus.Dead);
            return;
        }
        
        if (Directory.Exists(WORKDIR))
        {
            Log("Deleting old directory");
            Directory.Delete(WORKDIR, true);
        }
        
        Log("Creating new directory");
        Utils.CopyDirectory(Storage.TEMPLATEDIR, WORKDIR, true);

        // Hack. Unsure how to get around minecraft's aggressive caching otherwise
        string propertiesPath = Path.Join(WORKDIR, "server.properties");
        if (File.Exists(propertiesPath))
        {
            string content = await File.ReadAllTextAsync(propertiesPath);
            content = content.Replace("{{RESOURCE_URL}}",
                (mcServerMap?.HasResourcePack ?? false) ? $"{baseUrl}/Maps/resources/{HttpUtility.UrlEncode(mcServerMap.Name)}".Replace(":", "\\:") : "");
            await File.WriteAllTextAsync(propertiesPath, content);
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
                ChangeStatus(ServerStatus.Dead);
                return;
            }

            if (mcServerMap.ReadOnly)
            {
                Log("Copying read-only map");
                Utils.CopyDirectory(Path.GetFullPath(mcServerMap.Path), Path.Join(WORKDIR, "world"), true);
            }
            else
            {
                Log("Creating symlink for map");
                Directory.CreateSymbolicLink(Path.Join(WORKDIR, "world"), Path.GetFullPath(mcServerMap.Path));
            }
        }

        if (mcServerJar.Mappings.Count > 0)
        {
            Directory.CreateDirectory(Path.Join(WORKDIR, "world", "__saved"));
            Map(WORKDIR, Path.Join(WORKDIR, "world", "__saved"), mcServerJar.Mappings);
        }

        if (mcServerJar.ServerMappings.Count > 0)
        {
            Directory.CreateDirectory(Path.Join(Storage.SERVERMAPPINGSDIR, mcServerJar.Version));
            Map(WORKDIR, Path.Join(Storage.SERVERMAPPINGSDIR, mcServerJar.Version), mcServerJar.ServerMappings);
        }
        
        Log("Downloading new .jar");
        await _cache.RequestJars(WORKDIR, mcServerJar);
        await Launch(WORKDIR, mcServerJava);
        await _cache.DeleteJars(WORKDIR, mcServerJar);
        
        if (mcServerJar.Mappings.Count > 0)
            SaveUnmappedFiles(WORKDIR, Path.Join(WORKDIR, "world", "__saved"), mcServerJar.Mappings);
        
        if (mcServerJar.ServerMappings.Count > 0)
            SaveUnmappedFiles(WORKDIR, Path.Join(Storage.SERVERMAPPINGSDIR, mcServerJar.Version), mcServerJar.ServerMappings);
    }

    // Folders get mapped from src -> dst
    private void Map(string src, string dst, List<string> mappings)
    {
        foreach (var savedPath in mappings)
        {
            bool file = savedPath.Replace("\\.", "").Contains('.');
            string path = savedPath.Replace("\\.", ".");
            string srcPath = Path.Join(src, path);
            string dstPath = Path.GetFullPath(Path.Join(dst, path));
            
            if (path.Contains('/'))
            {
                string filename = Path.GetFileName(path);
                string containingDirName = Path.GetDirectoryName(path)!;

                Directory.CreateDirectory(Path.Join(src, containingDirName));
                dstPath = Path.GetFullPath(Path.Join(dst, filename));
            }

            if (file)
            {
                if (File.Exists(dstPath))
                    File.CreateSymbolicLink(srcPath, dstPath);
            }
            else
            {
                if (Directory.Exists(dstPath))
                    Directory.CreateSymbolicLink(srcPath, dstPath);
            }
        }
    }

    private void SaveUnmappedFiles(string src, string dst, List<string> mappings)
    {
        foreach (var savedPath in mappings)
        {
            bool file = savedPath.Replace("\\.", "").Contains('.');
            string path = savedPath.Replace("\\.", ".");
            string srcPath = Path.Join(src, path);
            string dstPath = Path.GetFullPath(Path.Join(dst, path));

            if (path.Contains('/'))
            {
                string filename = Path.GetFileName(path);
                dstPath = Path.GetFullPath(Path.Join(dst, filename));
            }

            if (file)
            {
                if (!File.Exists(dstPath) && File.Exists(srcPath))
                    File.Copy(srcPath, dstPath);
            }
            else
            {
                if (!Directory.Exists(dstPath) && Directory.Exists(srcPath))
                    Utils.CopyDirectory(srcPath, dstPath, true);
            }
        }
    }

    public async Task Launch(string workingDir, JavaTemplate java)
    {
        _terminal.WorkingDirectory = workingDir;
        ChangeStatus(ServerStatus.Started);
        Log("Starting server");
        bool result = await _terminal.Exec(java.Path, $"-Xmx{_config.MemoryInGb}G {_config.JavaFlags} -jar server.jar nogui");

        if (!result)
            ChangeStatus(ServerStatus.Dead);
        else
            ChangeStatus((_terminal.ExitCode == 0) ? ServerStatus.Stopped : ServerStatus.Dead);
    }

    public void Stop()
    {
        if (Status != ServerStatus.Ready)
            return;

        ChangeStatus(ServerStatus.Stopping);
        _terminal.WriteToStdIn("stop");
    }

    private void RunCommand(string command)
    {
        if (Status != ServerStatus.Ready)
            return;
        
        _terminal.WriteToStdIn(command);
    }

    private void MonitorStatusToReady(Terminal t, string s)
    {
        if (s.Contains("INFO]: Done (") || s.Contains("Can't keep up! Did the system time change, or is the server overloaded?") || s.Contains("[minecraft/DedicatedServer]: Done ("))
            ChangeStatus(ServerStatus.Ready);
    }
    
    private void MonitorPlayerList(Terminal t, string s)
    {
        if (s.Contains("joined the game"))
        {
            string[] split = s.Split("]:");

            string username = split[1].Split("joined")[0].Trim();
            
            if (!OnlinePlayers.Contains(username))
                OnlinePlayers.Add(username);
        }

        if (s.Contains("left the game"))
        {
            string[] split = s.Split("]:");
            OnlinePlayers.Remove(split[1].Split("left")[0].Trim());
        }
    }

    public void Log(string msg) => Console.WriteLine($"[Server] {msg}");

    private void ChangeStatus(ServerStatus status)
    {
        Status = status;
        OnStatusChange?.Invoke(status);
    }
}
