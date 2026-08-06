using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public static VehicleController Instance { get; private set; }

    private List<Vehicle> vehicles = new List<Vehicle>();

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

    public void RegisterVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        if (!vehicles.Contains(vehicle))
            vehicles.Add(vehicle);
    }

    public void UnregisterVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        vehicles.Remove(vehicle);
    }

    public bool RequestPath(Vehicle vehicle, Vector3 targetPosition)
    {
        if (GridSystem.Instance == null || PathfindingSystem.Instance == null)
            return false;

        if (!GridSystem.Instance.WorldToGrid(vehicle.position, out int startX, out int startY))
            return false;

        if (!GridSystem.Instance.WorldToGrid(targetPosition, out int targetX, out int targetY))
            return false;

        Vector2Int start = new Vector2Int(startX, startY);
        Vector2Int target = new Vector2Int(targetX, targetY);

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(start, target);
        if (path == null || path.Count == 0)
            return false;

        vehicle.SetPath(path);
        return true;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = vehicles.Count - 1; i >= 0; i--)
        {
            Vehicle vehicle = vehicles[i];
            if (!vehicle.isMoving) continue;

            Vector3 targetWaypoint = vehicle.GetNextWaypoint();
            Vector3 direction = targetWaypoint - vehicle.position;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                vehicle.position = targetWaypoint;
                vehicle.AdvanceToNextWaypoint();
            }
            else
            {
                float step = vehicle.speed * deltaTime;
                if (step >= distance)
                {
                    vehicle.position = targetWaypoint;
                    vehicle.AdvanceToNextWaypoint();
                }
                else
                {
                    vehicle.position += direction.normalized * step;
                }
            }
        }
    }
}
