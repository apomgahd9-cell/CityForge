using System.Collections.Generic;
using UnityEngine;

public class TrafficSystem : MonoBehaviour
{
    public static TrafficSystem Instance { get; private set; }

    private Dictionary<Vector2Int, int> vehicleCount = new Dictionary<Vector2Int, int>();
    private int defaultCapacity = 6;

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

    public void RegisterVehicleOnTile(Vector2Int tile)
    {
        if (!vehicleCount.ContainsKey(tile))
            vehicleCount[tile] = 0;

        vehicleCount[tile]++;
    }

    public void UnregisterVehicleFromTile(Vector2Int tile)
    {
        if (!vehicleCount.ContainsKey(tile)) return;

        vehicleCount[tile]--;
        if (vehicleCount[tile] <= 0)
            vehicleCount.Remove(tile);
    }

    public void UpdateVehicleTile(Vector2Int oldTile, Vector2Int newTile)
    {
        UnregisterVehicleFromTile(oldTile);
        RegisterVehicleOnTile(newTile);
    }

    public int GetVehicleCount(Vector2Int tile)
    {
        vehicleCount.TryGetValue(tile, out int count);
        return count;
    }

    public float GetTrafficLevel(Vector2Int tile)
    {
        int count = GetVehicleCount(tile);
        if (count == 0) return 0f;

        int capacity = GetRoadCapacity(tile);
        if (capacity <= 0) return 1f;

        return Mathf.Clamp01((float)count / capacity);
    }

    public float GetTrafficCost(Vector2Int tile)
    {
        float trafficLevel = GetTrafficLevel(tile);
        float baseTrafficCost = 2f;
        return trafficLevel * baseTrafficCost;
    }

    private int GetRoadCapacity(Vector2Int tile)
    {
        if (RoadNetwork.Instance != null)
        {
            RoadDefinition def = RoadNetwork.Instance.GetRoadDefinition(tile.x, tile.y);
            if (def != null)
                return def.capacity;
        }

        return defaultCapacity;
    }
}
