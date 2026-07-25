using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

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
        var data = new SaveData
        {
            saveName = fileName,
            saveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        foreach (var s in saveables)
            s.Save(data);

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string fullPath = Path.Combine(saveFolderPath, $"{fileName}.json");
        File.WriteAllText(fullPath, json);
        Debug.Log($"Game saved to: {fullPath}");
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
        var data = JsonConvert.DeserializeObject<SaveData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse save file.");
            return;
        }

        if (data.version != 1)
            Debug.LogWarning("Save file version mismatch!");

        // تحميل البيانات بالترتيب المضمون
        foreach (var s in saveables)
            s.Load(data);

        // إعادة بناء المقاييس المشتقة
        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.RecalculateAll();

        // TODO: عند تطبيق ISaveable على ServiceSystem، أضف السطر التالي:
        // if (ServiceSystem.Instance != null)
        //     ServiceSystem.Instance.RecalculateAllEffects();

        Debug.Log($"Game loaded from: {fullPath}");
    }

    public List<string> GetSaveFiles()
    {
        var files = new List<string>();
        if (Directory.Exists(saveFolderPath))
        {
            foreach (var f in Directory.GetFiles(saveFolderPath, "*.json"))
                files.Add(Path.GetFileNameWithoutExtension(f));
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
