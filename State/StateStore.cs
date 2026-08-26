using System.Text.Json;

namespace DicresPhotosUploader.State;

public class StateStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppState Load()
    {
        if (!File.Exists(path))
        {
            return new AppState();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
    }

    public void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, path, overwrite: true);
    }
}
