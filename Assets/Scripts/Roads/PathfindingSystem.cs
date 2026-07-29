using System.Collections.Generic;
using UnityEngine;

public class PathfindingSystem : MonoBehaviour
{
    public static PathfindingSystem Instance { get; private set; }

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

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        if (RoadGraph.Instance == null)
        {
            Debug.LogError("RoadGraph not found.");
            return new List<Vector2Int>();
        }

        if (!RoadGraph.Instance.IsGraphBuilt)
        {
            Debug.LogError("RoadGraph has not been built. Call BuildGraph() first.");
            return new List<Vector2Int>();
        }

        RoadNode startNode = RoadGraph.Instance.GetNode(start.x, start.y);
        RoadNode targetNode = RoadGraph.Instance.GetNode(target.x, target.y);

        if (startNode == null)
        {
            Debug.LogWarning($"Start node ({start.x}, {start.y}) is not a road.");
            return new List<Vector2Int>();
        }

        if (targetNode == null)
        {
            Debug.LogWarning($"Target node ({target.x}, {target.y}) is not a road.");
            return new List<Vector2Int>();
        }

        // A* Algorithm
        Dictionary<RoadNode, float> gCost = new Dictionary<RoadNode, float>();
        Dictionary<RoadNode, float> fCost = new Dictionary<RoadNode, float>();
        Dictionary<RoadNode, RoadNode> cameFrom = new Dictionary<RoadNode, RoadNode>();
        List<RoadNode> openSet = new List<RoadNode>();
        HashSet<RoadNode> closedSet = new HashSet<RoadNode>();

        gCost[startNode] = 0f;
        fCost[startNode] = Heuristic(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            RoadNode current = GetLowestFCost(openSet, fCost);

            if (current == targetNode)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (RoadEdge edge in current.neighbors)
            {
                RoadNode neighbor = edge.target;

                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = gCost[current] + edge.cost;

                if (!gCost.ContainsKey(neighbor) || tentativeGCost < gCost[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gCost[neighbor] = tentativeGCost;
                    fCost[neighbor] = gCost[neighbor] + Heuristic(neighbor, targetNode);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("No path found.");
        return new List<Vector2Int>();
    }

    private float Heuristic(RoadNode a, RoadNode b)
    {
        return Vector2Int.Distance(a.position, b.position);
    }

    private RoadNode GetLowestFCost(List<RoadNode> openSet, Dictionary<RoadNode, float> fCost)
    {
        RoadNode lowest = openSet[0];
        float lowestCost = fCost.ContainsKey(lowest) ? fCost[lowest] : float.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            RoadNode node = openSet[i];
            float cost = fCost.ContainsKey(node) ? fCost[node] : float.MaxValue;

            if (cost < lowestCost)
            {
                lowestCost = cost;
                lowest = node;
            }
        }

        return lowest;
    }

    private List<Vector2Int> ReconstructPath(Dictionary<RoadNode, RoadNode> cameFrom, RoadNode current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        path.Add(current.position);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current.position);
        }

        path.Reverse();
        return path;
    }
}
