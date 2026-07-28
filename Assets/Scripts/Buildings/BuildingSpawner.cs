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
            {
                instance.SetLevel(saved.currentLevel);

                BuildingDefinition def = instance.Definition;
                int areaWidth = def.size != null ? def.size.width : 1;
                int areaDepth = def.size != null ? def.size.depth : 1;

                if (OccupancyMap.Instance != null && GridSystem.Instance != null)
                {
                    if (GridSystem.Instance.WorldToGrid(position, out int gridX, out int gridY))
                    {
                        int occupantId = OccupancyMap.Instance.ReoccupyArea(gridX, gridY, areaWidth, areaDepth);
                        instance.SetOccupantId(occupantId);

                        TileType tileType = GetTileTypeForDefinition(def);

                        for (int x = gridX; x < gridX + areaWidth; x++)
                        {
                            for (int y = gridY; y < gridY + areaDepth; y++)
                            {
                                GridSystem.Instance.SetTile(x, y, new TileData
                                {
                                    gridX = x,
                                    gridY = y,
                                    type = tileType
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    private TileType GetTileTypeForDefinition(BuildingDefinition definition)
    {
        if (definition.zoneTags != null)
        {
            if (definition.zoneTags.Contains("residential"))
                return TileType.Residential;
            if (definition.zoneTags.Contains("commercial"))
                return TileType.Commercial;
            if (definition.zoneTags.Contains("industrial"))
                return TileType.Industrial;
        }

        return TileType.Service;
    }
}
