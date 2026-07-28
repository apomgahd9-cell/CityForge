using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour, ISaveable
{
    public static BuildingSpawner Instance { get; private set; }

    private List<BuildingInstance> activeBuildings = new List<BuildingInstance>();

    public int LoadPriority => 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Unregister(this);
    }

    public BuildingInstance SpawnBuilding(string buildingId)
    {
        return SpawnBuilding(buildingId, Vector3.zero);
    }

    public BuildingInstance SpawnBuilding(string buildingId, Vector3 position)
    {
        if (DataRegistry.Instance == null)
        {
            Debug.LogError("DataRegistry not found.");
            return null;
        }

        BuildingDefinition definition = DataRegistry.Instance.GetBuilding(buildingId);
        if (definition == null)
        {
            Debug.LogError($"Building definition not found: {buildingId}");
            return null;
        }

        BuildingInstance instance = new BuildingInstance(definition, position);
        activeBuildings.Add(instance);

        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.AddBuilding(instance);
        else
            Debug.LogWarning("MetricsSystem not available. Building not added to metrics.");

        Debug.Log($"Building spawned: {definition.displayName} (ID: {definition.id}) at {position}");
        return instance;
    }

    public IReadOnlyList<BuildingInstance> GetAllBuildings()
    {
        return activeBuildings.AsReadOnly();
    }

    public void RemoveBuilding(BuildingInstance building)
    {
        if (!activeBuildings.Contains(building)) return;

        activeBuildings.Remove(building);
        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.RemoveBuilding(building);

        Debug.Log($"Building removed: {building.Definition.displayName}");
    }

    public void Save(SaveData data)
    {
        data.buildings.Clear();
        foreach (BuildingInstance building in activeBuildings)
        {
            data.buildings.Add(new BuildingSaveData
            {
                definitionId = building.Definition.id,
                currentLevel = building.CurrentLevel,
                positionX = building.Position.x,
                positionY = building.Position.y,
                positionZ = building.Position.z
            });
        }
    }

    public void Load(SaveData data)
    {
        while (activeBuildings.Count > 0)
            RemoveBuilding(activeBuildings[0]);

        foreach (BuildingSaveData saved in data.buildings)
        {
            Vector3 position = new Vector3(saved.positionX, saved.positionY, saved.positionZ);
            BuildingInstance instance = SpawnBuilding(saved.definitionId, position);
            if (instance != null)
                instance.SetLevel(saved.currentLevel);
        }
    }
}
