using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

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

    public bool PlaceBuilding(string buildingId, Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        if (BuildingSpawner.Instance == null)
        {
            Debug.LogError("BuildingSpawner not found.");
            return false;
        }

        if (OccupancyMap.Instance == null)
        {
            Debug.LogError("OccupancyMap not found.");
            return false;
        }

        if (DataRegistry.Instance == null)
        {
            Debug.LogError("DataRegistry not found.");
            return false;
        }

        BuildingDefinition definition = DataRegistry.Instance.GetBuilding(buildingId);
        if (definition == null)
        {
            Debug.LogError($"Building definition not found: {buildingId}");
            return false;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out int gridX, out int gridY))
        {
            Debug.LogWarning("Position is outside grid bounds.");
            return false;
        }

        int areaWidth = definition.size != null ? definition.size.width : 1;
        int areaDepth = definition.size != null ? definition.size.depth : 1;

        if (!OccupancyMap.Instance.IsAreaFree(gridX, gridY, areaWidth, areaDepth))
        {
            Debug.LogWarning($"Area ({gridX},{gridY}) {areaWidth}x{areaDepth} is not free.");
            return false;
        }

        BuildingInstance instance = BuildingSpawner.Instance.SpawnBuilding(buildingId, worldPosition);
        if (instance == null)
        {
            Debug.LogError($"Failed to spawn building: {buildingId}");
            return false;
        }

        int occupantId = OccupancyMap.Instance.OccupyArea(gridX, gridY, areaWidth, areaDepth);
        if (occupantId < 0)
        {
            Debug.LogError("Failed to occupy area.");
            BuildingSpawner.Instance.RemoveBuilding(instance);
            return false;
        }

        instance.SetOccupantId(occupantId);

        TileType tileType = GetTileTypeForBuilding(definition);

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

        Debug.Log($"Placed {definition.displayName} at ({gridX}, {gridY}), size: {areaWidth}x{areaDepth}, occupant ID: {occupantId}");
        return true;
    }

    public bool PlaceZone(TileType zoneType, Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        if (OccupancyMap.Instance == null)
        {
            Debug.LogError("OccupancyMap not found.");
            return false;
        }

        if (zoneType != TileType.Residential && zoneType != TileType.Commercial && zoneType != TileType.Industrial)
        {
            Debug.LogWarning($"Invalid zone type: {zoneType}");
            return false;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out int gridX, out int gridY))
        {
            Debug.LogWarning("Position is outside grid bounds.");
            return false;
        }

        if (OccupancyMap.Instance.IsOccupied(gridX, gridY))
        {
            Debug.LogWarning($"Tile ({gridX}, {gridY}) is already occupied.");
            return false;
        }

        OccupancyMap.Instance.OccupyArea(gridX, gridY, 1, 1);

        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = zoneType
        });

        Debug.Log($"Zone {zoneType} placed at ({gridX}, {gridY})");
        return true;
    }

    public void RemovePlacement(BuildingInstance instance)
    {
        if (instance == null) return;

        if (OccupancyMap.Instance != null && instance.OccupantId > 0)
            OccupancyMap.Instance.FreeAreaByOccupantId(instance.OccupantId);

        if (GridSystem.Instance != null)
        {
            BuildingDefinition definition = instance.Definition;
            Vector3 pos = instance.Position;

            if (GridSystem.Instance.WorldToGrid(pos, out int gridX, out int gridY))
            {
                int areaWidth = definition.size != null ? definition.size.width : 1;
                int areaDepth = definition.size != null ? definition.size.depth : 1;

                for (int x = gridX; x < gridX + areaWidth; x++)
                {
                    for (int y = gridY; y < gridY + areaDepth; y++)
                    {
                        GridSystem.Instance.SetTile(x, y, new TileData
                        {
                            gridX = x,
                            gridY = y,
                            type = TileType.Empty
                        });
                    }
                }
            }
        }

        if (BuildingSpawner.Instance != null)
            BuildingSpawner.Instance.RemoveBuilding(instance);

        Debug.Log($"Placement removed for building at {instance.Position}");
    }

    private TileType GetTileTypeForBuilding(BuildingDefinition definition)
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
