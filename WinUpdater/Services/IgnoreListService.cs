using System.IO;
using System.Text.Json;

namespace WinUpdater.Services;

public class IgnoreListService
{
    private readonly string filePath;
    private readonly HashSet<string> ignoredIds;

    // Limits gegen manipulierte ignore.json
    private const long MaxFileSizeBytes = 1_048_576; // 1 MB
    private const int MaxEntries = 10_000;
    private const int MaxIdLength = 200;

    public IgnoreListService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinUpdater");
        Directory.CreateDirectory(folder);
        filePath = Path.Combine(folder, "ignore.json");
        ignoredIds = Load();
    }

    public IReadOnlySet<string> IgnoredIds => ignoredIds;

    public bool IsIgnored(string id) =>
        ignoredIds.Contains(id, StringComparer.OrdinalIgnoreCase);

    public void Add(string id)
    {
        // Ungültige IDs ablehnen
        if (string.IsNullOrWhiteSpace(id) || id.Length > MaxIdLength) return;

        ignoredIds.Add(id);
        Save();
    }

    public void Remove(string id)
    {
        ignoredIds.Remove(id);
        Save();
    }

    private HashSet<string> Load()
    {
        try
        {
            if (!File.Exists(filePath))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Datei-Größe prüfen, bevor sie gelesen wird
            var info = new FileInfo(filePath);
            if (info.Length > MaxFileSizeBytes)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(filePath);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();

            // Anzahl und Länge der Einträge begrenzen
            return new HashSet<string>(
                list.Where(s => !string.IsNullOrWhiteSpace(s) && s.Length <= MaxIdLength)
                    .Take(MaxEntries),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                new List<string>(ignoredIds),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            /* niemals crashen wegen Ignore-Liste */
        }
    }
}