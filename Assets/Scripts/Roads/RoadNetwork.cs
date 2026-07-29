using System.Collections.Generic;
using UnityEngine;

public class RoadNetwork : MonoBehaviour, ISaveable
{
    public static RoadNetwork Instance { get; private set; }

    private HashSet<Vector2Int> roadTiles = new HashSet<Vector2Int>();

    public int LoadPriority => 10;

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

    public bool AddRoad(int gridX, int gridY)
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
        if (roadTiles.Contains(pos))
        {
            Debug.LogWarning($"Road already exists at ({gridX}, {gridY})");
            return false;
        }

        roadTiles.Add(pos);
        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = TileType.Road
        });

        return true;
    }

    public bool RemoveRoad(int gridX, int gridY)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return false;
        }

        Vector2Int pos = new Vector2Int(gridX, gridY);
        if (!roadTiles.Contains(pos))
        {
            Debug.LogWarning($"No road at ({gridX}, {gridY})");
            return false;
        }

        roadTiles.Remove(pos);
        GridSystem.Instance.SetTile(gridX, gridY, new TileData
        {
            gridX = gridX,
            gridY = gridY,
            type = TileType.Empty
        });

        return true;
    }

    public bool IsRoad(int gridX, int gridY)
    {
        if (GridSystem.Instance != null &&
            !GridSystem.Instance.IsValidGridPosition(gridX, gridY))
            return false;

        return roadTiles.Contains(new Vector2Int(gridX, gridY));
    }

    public List<Vector2Int> GetNeighbors(int gridX, int gridY)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            int nx = gridX + dir.x;
            int ny = gridY + dir.y;

            if (IsRoad(nx, ny))
                neighbors.Add(new Vector2Int(nx, ny));
        }

        return neighbors;
    }

    public int RoadCount => roadTiles.Count;

    public void Save(SaveData data)
    {
        if (data.roads == null)
            data.roads = new List<RoadSaveData>();
        else
            data.roads.Clear();

        foreach (Vector2Int pos in roadTiles)
        {
            data.roads.Add(new RoadSaveData
            {
                type = "basic_road",
                gridX = pos.x,
                gridY = pos.y
            });
        }
    }

    public void Load(SaveData data)
    {
        roadTiles.Clear();

        if (data.roads == null) return;

        foreach (RoadSaveData saved in data.roads)
        {
            Vector2Int pos = new Vector2Int(saved.gridX, saved.gridY);
            roadTiles.Add(pos);

            if (GridSystem.Instance != null)
            {
                GridSystem.Instance.SetTile(saved.gridX, saved.gridY, new TileData
                {
                    gridX = saved.gridX,
                    gridY = saved.gridY,
                    type = TileType.Road
                });
            }
        }
    }
}
