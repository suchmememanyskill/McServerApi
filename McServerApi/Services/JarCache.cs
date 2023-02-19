using System.Net;
using System.Runtime.InteropServices;
using McServerApi.Model;

namespace McServerApi.Services;

public class JarCache
{
    private Storage _storage;

    public JarCache(Storage storage)
    {
        _storage = storage;
    }

    public async Task RequestJars(string workdir, string version)
    {
        var server = _storage.Servers.Find(x => x.Version == version);
        if (server == null)
            throw new ArgumentException("Version not found");

        await RequestJars(workdir, server);
    }

    public async Task RequestJars(string workdir, ServerTemplate server)
    {
        foreach (var (key, value) in server.Downloadables)
        {
            await RequestJar(Path.Join(workdir, key), value);
        }
    }

    public async Task DeleteJars(string workdir, ServerTemplate server)
    {
        foreach (var (key, value) in server.Downloadables)
        {
            string path = Path.Join(workdir, key);
            
            if (File.Exists(path))
                File.Delete(path);
        }
    }
    
    private async Task RequestJar(string path, string url)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        
        string cacheName = Utils.CreateMD5(url);
        string cachePath = Path.Join(Storage.JARCACHEDIR, cacheName);

        if (!Directory.Exists(Storage.JARCACHEDIR))
            Directory.CreateDirectory(Storage.JARCACHEDIR);

        if (!File.Exists(cachePath))
        {
            using (HttpClient client = new())
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Invalid server url");
                }

                await using (var fs = new FileStream(cachePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }
        
        File.CreateSymbolicLink(path, Path.GetFullPath(cachePath));
    }
}