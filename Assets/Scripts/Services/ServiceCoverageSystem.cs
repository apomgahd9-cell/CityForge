using System.Collections.Generic;
using UnityEngine;

public class ServiceCoverageSystem : MonoBehaviour
{
    public static ServiceCoverageSystem Instance { get; private set; }

    private Dictionary<string, HashSet<Vector2Int>> coverage = new Dictionary<string, HashSet<Vector2Int>>();
    private ServicesData servicesData;

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
        servicesData = JsonLoader.Load<ServicesData>("Data/Services/services");

        if (servicesData == null)
        {
            Debug.LogWarning("Failed to load services.json. Using fallback service distances.");
        }
    }

    public void RebuildCoverage()
    {
        BuildAllCoverage();
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
            string serviceId = kvp.Key;
            List<Vector2Int> origins = kvp.Value;

            HashSet<Vector2Int> coveredTiles = new HashSet<Vector2Int>();

            foreach (Vector2Int origin in origins)
            {
                int maxDistance = GetMaxDistance(serviceId);
                HashSet<Vector2Int> fromOrigin = BFS(origin, maxDistance);
                coveredTiles.UnionWith(fromOrigin);
            }

            coverage[serviceId] = coveredTiles;
        }

        ServiceSystem.Instance?.RecalculateAllEffects();

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
        Dictionary<string, List<Vector2Int>> locations = new Dictionary<string, List<Vector2Int>>();

        if (BuildingSpawner.Instance == null)
        {
            Debug.LogWarning("BuildingSpawner not found.");
            return locations;
        }

        if (GridSystem.Instance == null)
        {
            Debug.LogWarning("GridSystem not found.");
            return locations;
        }

        foreach (BuildingInstance building in BuildingSpawner.Instance.GetAllBuildings())
        {
            BuildingDefinition def = building.Definition;
            if (def.services == null || def.services.Count == 0) continue;

            if (!GridSystem.Instance.WorldToGrid(building.Position, out int gridX, out int gridY))
                continue;

            foreach (ServiceOutput service in def.services)
            {
                if (service.amount <= 0) continue;

                if (!locations.ContainsKey(service.serviceId))
                    locations[service.serviceId] = new List<Vector2Int>();

                locations[service.serviceId].Add(new Vector2Int(gridX, gridY));
            }
        }

        return locations;
    }

    private int GetMaxDistance(string serviceId)
    {
        if (servicesData != null && servicesData.services != null &&
            servicesData.services.TryGetValue(serviceId, out ServiceDefinition def))
        {
            return def.radius;
        }

        return 10;
    }

    public HashSet<Vector2Int> GetCoverage(string serviceId)
    {
        coverage.TryGetValue(serviceId, out HashSet<Vector2Int> result);
        return result ?? new HashSet<Vector2Int>();
    }

    public bool IsCovered(int gridX, int gridY, string serviceId)
    {
        return GetCoverage(serviceId).Contains(new Vector2Int(gridX, gridY));
    }
}
