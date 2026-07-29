using System.Collections.Generic;
using UnityEngine;

public class ServiceCoverageSystem : MonoBehaviour
{
    public static ServiceCoverageSystem Instance { get; private set; }

    private Dictionary<string, HashSet<Vector2Int>> coverage = new Dictionary<string, HashSet<Vector2Int>>();

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

    public void BuildAllCoverage()
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogError("GridSystem not found.");
            return;
        }

        if (RoadGraph.Instance == null)
        {
            Debug.LogError("RoadGraph not found.");
            return;
        }

        if (!RoadGraph.Instance.IsGraphBuilt)
        {
            Debug.LogWarning("RoadGraph not built. Call BuildGraph() first.");
            return;
        }

        coverage.Clear();

        Dictionary<string, List<Vector2Int>> serviceLocations = GetServiceLocations();

        foreach (var kvp in serviceLocations)
        {
            string serviceType = kvp.Key;
            List<Vector2Int> origins = kvp.Value;

            HashSet<Vector2Int> coveredTiles = new HashSet<Vector2Int>();

            foreach (Vector2Int origin in origins)
            {
                HashSet<Vector2Int> fromOrigin = BFS(origin, GetMaxDistance(serviceType));
                coveredTiles.UnionWith(fromOrigin);
            }

            coverage[serviceType] = coveredTiles;
        }

        Debug.Log($"ServiceCoverage built for {coverage.Count} service types.");
    }

    private HashSet<Vector2Int> BFS(Vector2Int start, int maxDistance)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        visited.Add(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDist = distances[current];

            if (currentDist >= maxDistance)
                continue;

            RoadNode node = RoadGraph.Instance.GetNode(current.x, current.y);
            if (node == null) continue;

            foreach (RoadEdge edge in node.neighbors)
            {
                Vector2Int neighborPos = edge.target.position;
                if (!visited.Contains(neighborPos))
                {
                    visited.Add(neighborPos);
                    distances[neighborPos] = currentDist + 1;
                    queue.Enqueue(neighborPos);
                }
            }
        }

        return visited;
    }

    private Dictionary<string, List<Vector2Int>> GetServiceLocations()
    {
        // TODO: جلب مواقع الخدمات من ServiceSystem → BuildingSpawner → BuildingInstance.Definition
        return new Dictionary<string, List<Vector2Int>>
        {
            { "electricity", new List<Vector2Int>() },
            { "water", new List<Vector2Int>() },
            { "police", new List<Vector2Int>() },
            { "fire", new List<Vector2Int>() },
            { "health", new List<Vector2Int>() },
            { "education", new List<Vector2Int>() }
        };
    }

    private int GetMaxDistance(string serviceType)
    {
        // TODO: استرجاع maxDistance من ServiceDefinition لاحقاً
        return serviceType switch
        {
            "electricity" => 15,
            "water" => 15,
            "police" => 12,
            "fire" => 10,
            "health" => 15,
            "education" => 14,
            _ => 10
        };
    }

    public HashSet<Vector2Int> GetCoverage(string serviceType)
    {
        coverage.TryGetValue(serviceType, out HashSet<Vector2Int> result);
        return result ?? new HashSet<Vector2Int>();
    }

    public bool IsCovered(int gridX, int gridY, string serviceType)
    {
        return GetCoverage(serviceType).Contains(new Vector2Int(gridX, gridY));
    }
}
