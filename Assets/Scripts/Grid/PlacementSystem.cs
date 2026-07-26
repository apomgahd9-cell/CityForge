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

        TileData tile = GridSystem.Instance.GetTile(gridX, gridY);
        if (tile.type != TileType.Empty)
        {
            Debug.LogWarning($"Tile ({gridX}, {gridY}) is already occupied by {tile.type}.");
            return false;
        }

        BuildingInstance instance = BuildingSpawner.Instance.SpawnBuilding(buildingId);
        if (instance == null)
        {
            Debug.LogError($"Failed to spawn building: {buildingId}");
            return false;
        }

        TileType tileType = GetTileTypeForBuilding(definition);
        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = tileType
        });

        Debug.Log($"Placed {definition.displayName} at ({gridX}, {gridY})");
        return true;
    }

    public bool PlaceZone(TileType zoneType, Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
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

        TileData tile = GridSystem.Instance.GetTile(gridX, gridY);
        if (tile.type != TileType.Empty)
        {
            Debug.LogWarning($"Tile ({gridX}, {gridY}) is already occupied by {tile.type}.");
            return false;
        }

        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = zoneType
        });

        Debug.Log($"Zone {zoneType} placed at ({gridX}, {gridY})");
        return true;
    }

    public void RemovePlacement(Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out int gridX, out int gridY))
        {
            Debug.LogWarning("Position is outside grid bounds.");
            return;
        }

        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = TileType.Empty
        });

        Debug.Log($"Cleared tile at ({gridX}, {gridY})");
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
