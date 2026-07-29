using System.Collections.Generic;
using UnityEngine;

public class RoadGraph : MonoBehaviour
{
    public static RoadGraph Instance { get; private set; }

    private Dictionary<Vector2Int, RoadNode> nodes = new Dictionary<Vector2Int, RoadNode>();
    private bool graphBuilt;

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

    public void BuildGraph()
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found. Cannot build graph.");
            return;
        }

        if (RoadNetwork.Instance == null)
        {
            Debug.LogError("RoadNetwork not found. Cannot build graph.");
            return;
        }

        nodes.Clear();

        for (int x = 0; x < GridSystem.Instance.Width; x++)
        {
            for (int y = 0; y < GridSystem.Instance.Height; y++)
            {
                if (RoadNetwork.Instance.IsRoad(x, y))
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    nodes[pos] = new RoadNode { position = pos };
                }
            }
        }

        foreach (var kvp in nodes)
        {
            RoadNode node = kvp.Value;
            List<Vector2Int> neighborPositions = RoadNetwork.Instance.GetNeighbors(node.position.x, node.position.y);

            foreach (Vector2Int neighborPos in neighborPositions)
            {
                if (nodes.TryGetValue(neighborPos, out RoadNode neighborNode))
                {
                    float cost = CalculateEdgeCost(node.position, neighborPos);
                    node.neighbors.Add(new RoadEdge
                    {
                        target = neighborNode,
                        cost = cost
                    });
                }
            }
        }

        graphBuilt = true;
        Debug.Log($"RoadGraph built with {nodes.Count} nodes.");
    }

    public void Rebuild()
    {
        ClearGraph();
        BuildGraph();
    }

    private float CalculateEdgeCost(Vector2Int from, Vector2Int to)
    {
        // TODO: استرجاع RoadDefinition الفعلي من DataRegistry لاحقاً
        float baseSpeed = 40f;
        float distance = Vector2Int.Distance(from, to);
        return distance / baseSpeed;
    }

    public RoadNode GetNode(int gridX, int gridY)
    {
        nodes.TryGetValue(new Vector2Int(gridX, gridY), out RoadNode node);
        return node;
    }

    public bool IsGraphBuilt => graphBuilt;
    public int NodeCount => nodes.Count;

    public void ClearGraph()
    {
        nodes.Clear();
        graphBuilt = false;
    }
}

public class RoadNode
{
    public Vector2Int position;
    public List<RoadEdge> neighbors = new List<RoadEdge>();
}

public class RoadEdge
{
    public RoadNode target;
    public float cost;
}
