using System.Text.Json;

namespace DicresPhotosUploader.State;

/// <summary>
/// Stores the run history (max. <see cref="MaxEntries"/>, the oldest ones are discarded).
/// Same atomic write pattern (temp file + rename) as <see cref="StateStore"/>.
/// </summary>
public class RunHistoryStore
{
    private const int MaxEntries = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public RunHistoryStore(string path)
    {
        _path = path;
    }

    public List<RunHistoryEntry> Load()
    {
        if (!File.Exists(_path))
        {
            return new List<RunHistoryEntry>();
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<RunHistoryEntry>>(json) ?? new List<RunHistoryEntry>();
    }

    public void Append(RunHistoryEntry entry)
    {
        var entries = Load();
        entries.Add(entry);

        if (entries.Count > MaxEntries)
        {
            entries = entries.Skip(entries.Count - MaxEntries).ToList();
        }

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        var tmpPath = _path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, _path, overwrite: true);
    }
}
