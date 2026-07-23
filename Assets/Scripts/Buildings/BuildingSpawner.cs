using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    public static BuildingSpawner Instance { get; private set; }

    private List<BuildingInstance> activeBuildings = new List<BuildingInstance>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // تم توحيد السلوك مع بقية الأنظمة
    }

    public BuildingInstance SpawnBuilding(string buildingId)
    {
        if (DataRegistry.Instance == null)
        {
            Debug.LogError("DataRegistry not found!");
            return null;
        }

        BuildingDefinition definition = DataRegistry.Instance.GetBuilding(buildingId);
        if (definition == null)
        {
            Debug.LogError($"Building definition not found: {buildingId}");
            return null;
        }

        BuildingInstance instance = new BuildingInstance(definition);
        activeBuildings.Add(instance);

        if (MetricsSystem.Instance != null)
        {
            MetricsSystem.Instance.AddBuilding(instance);
        }
        else
        {
            Debug.LogWarning("MetricsSystem not available. Building not added to metrics.");
        }

        Debug.Log($"Building spawned: {definition.displayName} (ID: {definition.id})");
        return instance;
    }

    // تم تغيير نوع الإرجاع لتجنب النسخ غير الضروري
    public IReadOnlyList<BuildingInstance> GetAllBuildings()
    {
        return activeBuildings.AsReadOnly();
    }

    public void RemoveBuilding(BuildingInstance building)
    {
        if (activeBuildings.Contains(building))
        {
            activeBuildings.Remove(building);
            if (MetricsSystem.Instance != null)
            {
                MetricsSystem.Instance.RemoveBuilding(building);
            }
            Debug.Log($"Building removed: {building.Definition.displayName}");
        }
    }
}
