using System.Collections.Generic;
using UnityEngine;

public class Vehicle
{
    public string id;
    public Vector3 position;
    public float speed;
    public List<Vector2Int> currentPath;
    public int pathIndex;
    public bool isMoving;
    public int lastRerouteTick;
    public int rerouteCooldownTicks = 60;

    public Vehicle(string id, Vector3 startPosition, float speed)
    {
        this.id = id;
        this.position = startPosition;
        this.speed = speed;
        this.currentPath = new List<Vector2Int>();
        this.pathIndex = 0;
        this.isMoving = false;
        this.lastRerouteTick = -rerouteCooldownTicks;
    }

    public void SetPath(List<Vector2Int> path)
    {
        currentPath = path;
        pathIndex = 0;
        isMoving = path != null && path.Count > 0;
    }

    public Vector3 GetNextWaypoint()
    {
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
            return position;

        if (GridSystem.Instance == null)
            return position;

        Vector2Int gridPos = currentPath[pathIndex];
        return GridSystem.Instance.GridToWorld(gridPos.x, gridPos.y);
    }

    public void AdvanceToNextWaypoint()
    {
        pathIndex++;
        if (pathIndex >= currentPath.Count)
        {
            isMoving = false;
            pathIndex = currentPath.Count;
        }
    }
}
