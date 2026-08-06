using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public static VehicleSpawner Instance { get; private set; }

    private int nextVehicleId;

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

    public Vehicle SpawnVehicle(Vector3 position, float speed = 20f)
    {
        string vehicleId = $"vehicle_{nextVehicleId++}";
        Vehicle vehicle = new Vehicle(vehicleId, position, speed);
        VehicleController.Instance?.RegisterVehicle(vehicle);
        Debug.Log($"Vehicle spawned: {vehicleId} at {position}");
        return vehicle;
    }

    public void DespawnVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        VehicleController.Instance?.UnregisterVehicle(vehicle);
        Debug.Log($"Vehicle despawned: {vehicle.id}");
    }
}
