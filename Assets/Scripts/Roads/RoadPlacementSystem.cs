using UnityEngine;

public class RoadPlacementSystem : MonoBehaviour
{
    public static RoadPlacementSystem Instance { get; private set; }

    // تعريف مؤقت للطريق الأساسي (لحين بناء roads.json و DataRegistry)
    private RoadDefinition defaultRoad = new RoadDefinition
    {
        id = "basic_road",
        displayName = "Basic Road",
        costPerTile = 10f,
        upkeepPerTile = 1f,
        speed = 40,
        lanes = 2
    };

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

    public bool PlaceRoad(Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        if (RoadNetwork.Instance == null)
        {
            Debug.LogError("RoadNetwork not found.");
            return false;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out int gridX, out int gridY))
        {
            Debug.LogWarning("Position is outside grid bounds.");
            return false;
        }

        if (RoadNetwork.Instance.IsRoad(gridX, gridY))
        {
            Debug.LogWarning($"Road already exists at ({gridX}, {gridY}).");
            return false;
        }

        if (OccupancyMap.Instance != null && OccupancyMap.Instance.IsOccupied(gridX, gridY))
        {
            Debug.LogWarning($"Tile ({gridX}, {gridY}) is occupied by a building.");
            return false;
        }

        // TODO: خصم تكلفة البناء من EconomySystem عند تثبيت واجهة الاقتصاد
        // if (EconomySystem.Instance != null && !EconomySystem.Instance.CanAfford(defaultRoad.costPerTile))
        // {
        //     Debug.LogWarning("Not enough funds to build road.");
        //     return false;
        // }

        bool placed = RoadNetwork.Instance.AddRoad(gridX, gridY);
        if (placed)
        {
            // TODO: EconomySystem.Instance.DeductFunds(defaultRoad.costPerTile);
            // TODO: إخطار RoadGraph و ServiceCoverage و Pathfinding بتحديث الشبكة
            Debug.Log($"Road placed at ({gridX}, {gridY})");
        }

        return placed;
    }

    public bool RemoveRoad(Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        if (RoadNetwork.Instance == null)
        {
            Debug.LogError("RoadNetwork not found.");
            return false;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out int gridX, out int gridY))
        {
            Debug.LogWarning("Position is outside grid bounds.");
            return false;
        }

        if (!RoadNetwork.Instance.IsRoad(gridX, gridY))
        {
            Debug.LogWarning($"No road at ({gridX}, {gridY}).");
            return false;
        }

        RoadNetwork.Instance.RemoveRoad(gridX, gridY);
        // TODO: إخطار RoadGraph و ServiceCoverage و Pathfinding بتحديث الشبكة
        Debug.Log($"Road removed at ({gridX}, {gridY})");
        return true;
    }
}
