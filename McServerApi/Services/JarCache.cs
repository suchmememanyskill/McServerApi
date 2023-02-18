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

    public async Task RequestJar(string dstPath, string version)
    {
        var server = _storage.Servers.Find(x => x.Version == version);
        if (server == null)
            throw new ArgumentException("Version not found");

        await RequestJar(dstPath, server);
    }
    
    public async Task RequestJar(string dstPath, ServerTemplate server)
    {
        string cacheName = $"{server.Version}.jar";
        string cachePath = Path.Join(Storage.JARCACHEDIR, cacheName);

        if (!Directory.Exists(Storage.JARCACHEDIR))
            Directory.CreateDirectory(Storage.JARCACHEDIR);

        if (!File.Exists(cachePath))
        {
            using (HttpClient client = new())
            {
                var response = await client.GetAsync(server.Url);
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

        // Symlinks aren't supported on windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            File.Copy(cachePath, dstPath);
        else
            File.CreateSymbolicLink(dstPath, Path.GetFullPath(cachePath));
    }
}