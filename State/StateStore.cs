using System.Text.Json;

namespace GooglePhotosUploader.State;

/// <summary>
/// Saves and loads the progress on disk. Writes to a temp file + rename
/// so a power outage or a Ctrl+C mid-write doesn't corrupt the state.
/// </summary>
public class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public StateStore(string path)
    {
        _path = path;
    }

    public AppState Load()
    {
        if (!File.Exists(_path))
        {
            return new AppState();
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
    }

    public void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var tmpPath = _path + ".tmp";
        File.WriteAllText(tmpPath, json);

        // File.Move with overwrite=true is atomic at the filesystem level
        // on modern Windows, Linux, and macOS.
        File.Move(tmpPath, _path, overwrite: true);
    }
}
