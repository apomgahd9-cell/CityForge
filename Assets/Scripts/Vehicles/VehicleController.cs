using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public static VehicleController Instance { get; private set; }

    private List<Vehicle> vehicles = new List<Vehicle>();
    private float safeDistance = 2f;

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
        {
            vehicles.Add(vehicle);
            RegisterOnTrafficSystem(vehicle);
        }
    }

    public void UnregisterVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        if (vehicles.Remove(vehicle))
        {
            UnregisterFromTrafficSystem(vehicle);
        }
    }

    private void RegisterOnTrafficSystem(Vehicle vehicle)
    {
        if (TrafficSystem.Instance == null || GridSystem.Instance == null) return;

        if (GridSystem.Instance.WorldToGrid(vehicle.position, out int gx, out int gy))
        {
            TrafficSystem.Instance.RegisterVehicleOnTile(new Vector2Int(gx, gy));
        }
    }

    private void UnregisterFromTrafficSystem(Vehicle vehicle)
    {
        if (TrafficSystem.Instance == null || GridSystem.Instance == null) return;

        if (GridSystem.Instance.WorldToGrid(vehicle.position, out int gx, out int gy))
        {
            TrafficSystem.Instance.UnregisterVehicleFromTile(new Vector2Int(gx, gy));
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = vehicles.Count - 1; i >= 0; i--)
        {
            Vehicle vehicle = vehicles[i];
            if (!vehicle.isMoving) continue;

            float currentSpeed = GetSafeSpeed(vehicle);

            Vector3 targetWaypoint = vehicle.GetNextWaypoint();
            Vector3 direction = targetWaypoint - vehicle.position;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                Vector2Int oldTile = GetGridPosition(vehicle.position);
                vehicle.position = targetWaypoint;
                vehicle.AdvanceToNextWaypoint();
                UpdateTrafficTile(vehicle, oldTile);
            }
            else
            {
                float step = currentSpeed * deltaTime;
                if (step >= distance)
                {
                    Vector2Int oldTile = GetGridPosition(vehicle.position);
                    vehicle.position = targetWaypoint;
                    vehicle.AdvanceToNextWaypoint();
                    UpdateTrafficTile(vehicle, oldTile);
                }
                else
                {
                    Vector2Int oldTile = GetGridPosition(vehicle.position);
                    vehicle.position += direction.normalized * step;
                    UpdateTrafficTile(vehicle, oldTile);
                }
            }
        }
    }

    private float GetSafeSpeed(Vehicle vehicle)
    {
        float baseSpeed = vehicle.speed;

        Vehicle frontVehicle = FindFrontVehicle(vehicle);
        if (frontVehicle != null)
        {
            float distanceToFront = Vector3.Distance(vehicle.position, frontVehicle.position);
            if (distanceToFront < safeDistance)
            {
                return 0f;
            }
            else if (distanceToFront < safeDistance * 2f)
            {
                return baseSpeed * 0.5f;
            }
        }

        return baseSpeed;
    }

    private Vehicle FindFrontVehicle(Vehicle vehicle)
    {
        Vehicle closest = null;
        float closestDistance = float.MaxValue;

        foreach (Vehicle other in vehicles)
        {
            if (other == vehicle) continue;
            if (!other.isMoving) continue;

            Vector3 toOther = other.position - vehicle.position;
            float distance = toOther.magnitude;

            Vector3 vehicleDirection = (vehicle.GetNextWaypoint() - vehicle.position).normalized;
            float dot = Vector3.Dot(vehicleDirection, toOther.normalized);

            if (dot > 0.5f && distance < closestDistance && distance < safeDistance * 3f)
            {
                closestDistance = distance;
                closest = other;
            }
        }

        return closest;
    }

    private Vector2Int GetGridPosition(Vector3 position)
    {
        if (GridSystem.Instance == null) return Vector2Int.zero;
        GridSystem.Instance.WorldToGrid(position, out int gx, out int gy);
        return new Vector2Int(gx, gy);
    }

    private void UpdateTrafficTile(Vehicle vehicle, Vector2Int oldTile)
    {
        if (TrafficSystem.Instance == null) return;

        Vector2Int newTile = GetGridPosition(vehicle.position);
        if (oldTile != newTile)
        {
            TrafficSystem.Instance.UpdateVehicleTile(oldTile, newTile);
        }
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
}
