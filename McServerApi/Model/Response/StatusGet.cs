using McServerApi.Services;

namespace McServerApi.Model;

public class StatusGet
{
    private Storage _storage;
    private Server _server;
    public MapTemplate? Map => _storage.Maps.Find(x => x.Name == _storage.CurrentConfiguration.MapName);
    public ServerTemplate? Version => _storage.Servers.Find(x => x.Version == _storage.CurrentConfiguration.ServerVersion);
    public string TextStatus => _server.Status.ToString();
    public List<string> OnlinePlayers => _server.OnlinePlayers;

    public StatusGet(Storage storage, Server server)
    {
        _storage = storage;
        _server = server;
    }
}