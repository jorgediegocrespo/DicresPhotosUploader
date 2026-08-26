using System.Text.Json;

namespace DicresPhotosUploader.State;

/// <summary>
/// Stores the run history (max. <see cref="MaxEntries"/>, the oldest ones are discarded).
/// Same atomic write pattern (temp file + rename) as <see cref="StateStore"/>.
/// </summary>
public class RunHistoryStore(string path)
{
    private const int MaxEntries = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public List<RunHistoryEntry> Load()
    {
        if (!File.Exists(path))
        {
            return new List<RunHistoryEntry>();
        }

        var json = File.ReadAllText(path);
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
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, path, overwrite: true);
    }
}
