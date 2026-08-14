using System.Collections.Generic;
using UnityEngine;

public class ZoneSystem : MonoBehaviour, ISaveable
{
    public static ZoneSystem Instance { get; private set; }

    private Dictionary<Vector2Int, ZoneData> zones = new Dictionary<Vector2Int, ZoneData>();

    public int LoadPriority => 15;

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

    public bool AddZone(int gridX, int gridY, ZoneType zoneType, int density = 1, int maxBuildings = 4)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        if (!GridSystem.Instance.IsValidGridPosition(gridX, gridY))
        {
            Debug.LogWarning($"Invalid grid position: ({gridX}, {gridY})");
            return false;
        }

        Vector2Int pos = new Vector2Int(gridX, gridY);
        if (zones.ContainsKey(pos))
        {
            Debug.LogWarning($"Zone already exists at ({gridX}, {gridY})");
            return false;
        }

        if (OccupancyMap.Instance != null && OccupancyMap.Instance.IsOccupied(gridX, gridY))
        {
            Debug.LogWarning($"Tile ({gridX}, {gridY}) is occupied by a building.");
            return false;
        }

        zones[pos] = new ZoneData
        {
            gridX = gridX,
            gridY = gridY,
            zoneType = zoneType,
            density = density,
            maxBuildings = maxBuildings
        };

        Debug.Log($"Zone {zoneType} added at ({gridX}, {gridY})");
        return true;
    }

    public bool RemoveZone(int gridX, int gridY)
    {
        Vector2Int pos = new Vector2Int(gridX, gridY);
        if (!zones.ContainsKey(pos))
        {
            Debug.LogWarning($"No zone at ({gridX}, {gridY})");
            return false;
        }

        zones.Remove(pos);
        Debug.Log($"Zone removed at ({gridX}, {gridY})");
        return true;
    }

    public ZoneData GetZone(int gridX, int gridY)
    {
        zones.TryGetValue(new Vector2Int(gridX, gridY), out ZoneData zone);
        return zone;
    }

    public bool HasZone(int gridX, int gridY)
    {
        return zones.ContainsKey(new Vector2Int(gridX, gridY));
    }

    public List<ZoneData> GetAllZones()
    {
        return new List<ZoneData>(zones.Values);
    }

    public List<ZoneData> GetZonesByType(ZoneType zoneType)
    {
        List<ZoneData> result = new List<ZoneData>();
        foreach (var zone in zones.Values)
        {
            if (zone.zoneType == zoneType)
                result.Add(zone);
        }
        return result;
    }

    public List<ZoneData> GetBuildableZones(ZoneType zoneType)
    {
        List<ZoneData> buildable = new List<ZoneData>();

        foreach (var zone in GetZonesByType(zoneType))
        {
            if (!HasRoadAccess(zone.gridX, zone.gridY))
                continue;

            int buildingCount = GetBuildingCount(zone.gridX, zone.gridY);
            if (buildingCount >= zone.maxBuildings)
                continue;

            buildable.Add(zone);
        }

        return buildable;
    }

    public bool HasRoadAccess(int gridX, int gridY)
    {
        if (RoadNetwork.Instance == null) return false;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            int nx = gridX + dir.x;
            int ny = gridY + dir.y;

            if (RoadNetwork.Instance.IsRoad(nx, ny))
                return true;
        }

        return false;
    }

    public int GetBuildingCount(int gridX, int gridY)
    {
        if (BuildingSpawner.Instance == null) return 0;

        int count = 0;
        foreach (BuildingInstance building in BuildingSpawner.Instance.GetAllBuildings())
        {
            if (GridSystem.Instance != null &&
                GridSystem.Instance.WorldToGrid(building.Position, out int bx, out int by))
            {
                if (bx == gridX && by == gridY)
                    count++;
            }
        }

        return count;
    }

    public int ZoneCount => zones.Count;

    public void Save(SaveData data)
    {
        if (data.zones == null)
            data.zones = new List<ZoneSaveData>();
        else
            data.zones.Clear();

        foreach (var kvp in zones)
        {
            ZoneData zone = kvp.Value;

            data.zones.Add(new ZoneSaveData
            {
                gridX = zone.gridX,
                gridY = zone.gridY,
                zoneType = zone.zoneType,
                density = zone.density,
                maxBuildings = zone.maxBuildings
            });
        }
    }

    public void Load(SaveData data)
    {
        zones.Clear();

        if (data.zones == null) return;

        foreach (ZoneSaveData saved in data.zones)
        {
            if (GridSystem.Instance != null &&
                !GridSystem.Instance.IsValidGridPosition(saved.gridX, saved.gridY))
            {
                Debug.LogWarning($"Skipping zone at invalid position: ({saved.gridX}, {saved.gridY})");
                continue;
            }

            Vector2Int pos = new Vector2Int(saved.gridX, saved.gridY);

            if (zones.ContainsKey(pos))
            {
                Debug.LogWarning($"Duplicate zone at ({saved.gridX}, {saved.gridY}). Skipping.");
                continue;
            }

            zones[pos] = new ZoneData
            {
                gridX = saved.gridX,
                gridY = saved.gridY,
                zoneType = saved.zoneType,
                density = saved.density,
                maxBuildings = saved.maxBuildings
            };
        }
    }
}
