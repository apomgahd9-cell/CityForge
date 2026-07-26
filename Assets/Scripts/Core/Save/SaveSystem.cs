using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private string saveFolderPath;
    private List<ISaveable> saveables = new List<ISaveable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFolderPath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(saveFolderPath))
            Directory.CreateDirectory(saveFolderPath);
    }

    public void Register(ISaveable saveable)
    {
        if (saveables.Contains(saveable)) return;

        saveables.Add(saveable);
        saveables.Sort((a, b) => a.LoadPriority.CompareTo(b.LoadPriority));
    }

    public void Unregister(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }

    public void SaveGame(string fileName)
    {
        SaveData data = new SaveData
        {
            saveName = fileName,
            saveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        foreach (ISaveable saveable in saveables)
            saveable.Save(data);

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string fullPath = Path.Combine(saveFolderPath, $"{fileName}.json");
        File.WriteAllText(fullPath, json);
        Debug.Log($"Game saved: {fullPath}");
    }

    public void LoadGame(string fileName)
    {
        string fullPath = Path.Combine(saveFolderPath, $"{fileName}.json");
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Save file not found: {fullPath}");
            return;
        }

        string json = File.ReadAllText(fullPath);
        SaveData data = JsonConvert.DeserializeObject<SaveData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse save file.");
            return;
        }

        if (data.version != 1)
            Debug.LogWarning("Save file version mismatch.");

        foreach (ISaveable saveable in saveables)
            saveable.Load(data);

        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.RecalculateAll();

        Debug.Log($"Game loaded: {fullPath}");
    }

    public List<string> GetSaveFiles()
    {
        List<string> files = new List<string>();
        if (Directory.Exists(saveFolderPath))
        {
            foreach (string file in Directory.GetFiles(saveFolderPath, "*.json"))
                files.Add(Path.GetFileNameWithoutExtension(file));
        }
        return files;
    }

    public void DeleteSave(string fileName)
    {
        string fullPath = Path.Combine(saveFolderPath, $"{fileName}.json");
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log($"Save deleted: {fullPath}");
        }
    }
}
