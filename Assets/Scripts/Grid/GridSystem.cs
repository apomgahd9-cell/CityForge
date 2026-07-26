using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private int width = 128;
    [SerializeField] private int height = 128;
    [SerializeField] private float tileSize = 8f;
    [SerializeField] private Vector3 origin = Vector3.zero;

    private TileData[,] grid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        grid = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new TileData
                {
                    gridX = x,
                    gridY = y,
                    type = TileType.Empty
                };
            }
        }

        Debug.Log($"Grid initialized: {width}x{height}, tile size: {tileSize}");
    }

    public Vector3 GridToWorld(int gridX, int gridY)
    {
        return origin + new Vector3(gridX * tileSize, 0f, gridY * tileSize);
    }

    public bool WorldToGrid(Vector3 worldPosition, out int gridX, out int gridY)
    {
        gridX = Mathf.RoundToInt((worldPosition.x - origin.x) / tileSize);
        gridY = Mathf.RoundToInt((worldPosition.z - origin.z) / tileSize);

        return IsValidGridPosition(gridX, gridY);
    }

    public bool IsValidGridPosition(int gridX, int gridY)
    {
        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
    }

    public TileData GetTile(int gridX, int gridY)
    {
        if (!IsValidGridPosition(gridX, gridY))
        {
            Debug.LogWarning($"Invalid grid position: ({gridX}, {gridY})");
            return default;
        }

        return grid[gridX, gridY];
    }

    public void SetTile(int gridX, int gridY, TileData tile)
    {
        if (!IsValidGridPosition(gridX, gridY))
        {
            Debug.LogWarning($"Invalid grid position: ({gridX}, {gridY})");
            return;
        }

        tile.gridX = gridX;
        tile.gridY = gridY;
        grid[gridX, gridY] = tile;
    }

    public int Width => width;
    public int Height => height;
    public float TileSize => tileSize;
}
