using UnityEngine;

public class OccupancyMap : MonoBehaviour
{
    public static OccupancyMap Instance { get; private set; }

    private int[,] occupancyGrid;
    private int nextOccupantId = 1;
    private bool initialized;

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

    private void EnsureInitialized()
    {
        if (initialized) return;

        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found. Cannot initialize OccupancyMap.");
            return;
        }

        occupancyGrid = new int[GridSystem.Instance.Width, GridSystem.Instance.Height];
        initialized = true;
        Debug.Log("OccupancyMap initialized.");
    }

    public bool IsAreaFree(int startX, int startY, int areaWidth, int areaDepth)
    {
        EnsureInitialized();
        if (!initialized) return false;

        for (int x = startX; x < startX + areaWidth; x++)
        {
            for (int y = startY; y < startY + areaDepth; y++)
            {
                if (!GridSystem.Instance.IsValidGridPosition(x, y))
                    return false;

                if (occupancyGrid[x, y] != 0)
                    return false;
            }
        }

        return true;
    }

    public int OccupyArea(int startX, int startY, int areaWidth, int areaDepth)
    {
        EnsureInitialized();
        if (!initialized) return -1;

        if (!IsAreaFree(startX, startY, areaWidth, areaDepth))
        {
            Debug.LogWarning($"Area ({startX},{startY}) {areaWidth}x{areaDepth} is not free.");
            return -1;
        }

        int occupantId = nextOccupantId++;

        for (int x = startX; x < startX + areaWidth; x++)
        {
            for (int y = startY; y < startY + areaDepth; y++)
            {
                occupancyGrid[x, y] = occupantId;
            }
        }

        Debug.Log($"Area ({startX},{startY}) {areaWidth}x{areaDepth} occupied by ID {occupantId}");
        return occupantId;
    }

    public void FreeAreaByOccupantId(int occupantId)
    {
        EnsureInitialized();
        if (!initialized) return;
        if (occupantId <= 0) return;

        for (int x = 0; x < GridSystem.Instance.Width; x++)
        {
            for (int y = 0; y < GridSystem.Instance.Height; y++)
            {
                if (occupancyGrid[x, y] == occupantId)
                    occupancyGrid[x, y] = 0;
            }
        }

        Debug.Log($"All tiles for occupant ID {occupantId} freed.");
    }

    public bool IsOccupied(int gridX, int gridY)
    {
        EnsureInitialized();
        if (!initialized) return false;
        if (!GridSystem.Instance.IsValidGridPosition(gridX, gridY)) return false;

        return occupancyGrid[gridX, gridY] != 0;
    }

    public int GetOccupantId(int gridX, int gridY)
    {
        EnsureInitialized();
        if (!initialized) return -1;
        if (!GridSystem.Instance.IsValidGridPosition(gridX, gridY)) return -1;

        return occupancyGrid[gridX, gridY];
    }
}
