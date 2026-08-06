using System.Text.Json;

namespace GooglePhotosUploader.Config;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> in <c>config.json</c> inside <see cref="AppConfig.AppDataFolder"/>.
/// Same atomic write pattern (temp file + rename) as the rest of the stores.
/// </summary>
public class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;

    public ConfigStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppConfig.AppDataFolder, "config.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_path))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(AppConfig.AppDataFolder);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tmpPath = _path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, _path, overwrite: true);
    }
}
