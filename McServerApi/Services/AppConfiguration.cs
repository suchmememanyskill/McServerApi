namespace McServerApi.Services;

public class AppConfiguration
{
    private IConfiguration _root;
    public long Memory { get; private set; }
    public string JavaFlags { get; private set; }
    public long ApiPort { get; private set; }
    
    
    public AppConfiguration()
    {
        _root = new ConfigurationBuilder()
            .AddJsonFile("appsettings.example.json", optional: true)
            .AddJsonFile($"appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        SetValues();
    }

    private void SetValues()
    {
        Memory = GetInt("Config:Memory");
        JavaFlags = GetString("Config:JavaFlags");
        ApiPort = GetInt("Config:ApiPort");
    }
    
    private string GetString(string key)
    {
        string? value = _root[key];

        if (value == null)
            throw new Exception($"Could not find '{key}' in configuration");
        
        Console.WriteLine($"Reading configuration key '{key}'" + (key.StartsWith("Config") ? $". Got value '{value}'" : ""));
        return value;
    }

    private long GetInt(string key)
        => long.Parse(GetString(key));

    private bool GetBool(string key)
        => GetString(key).ToLower() == "true";

    private List<string> GetList(string key)
        => GetString(key).Split(';').Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
}