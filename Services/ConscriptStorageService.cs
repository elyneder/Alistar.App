using System.IO;
using System.Text.Json;
using Alistar.App.Models;

namespace Alistar.App.Services;

public static class ConscriptStorageService
{
    private static readonly string StorageDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Alistar");

    private static readonly string StoragePath = Path.Combine(StorageDirectory, "conscripts.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static List<Conscript> GetAll()
    {
        EnsureStorageExists();

        var content = File.ReadAllText(StoragePath);

        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<Conscript>>(content, JsonOptions) ?? [];
    }

    public static void Add(Conscript conscript)
    {
        var conscripts = GetAll();
        conscripts.Add(conscript);
        SaveAll(conscripts);
    }

    private static void SaveAll(List<Conscript> conscripts)
    {
        Directory.CreateDirectory(StorageDirectory);
        var json = JsonSerializer.Serialize(conscripts, JsonOptions);
        File.WriteAllText(StoragePath, json);
    }

    private static void EnsureStorageExists()
    {
        Directory.CreateDirectory(StorageDirectory);

        if (!File.Exists(StoragePath))
        {
            File.WriteAllText(StoragePath, "[]");
        }
    }
}
