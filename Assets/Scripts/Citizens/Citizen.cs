using System.Collections.Generic;
using UnityEngine;

public enum CitizenState
{
    AtHome,
    GoingToWork,
    AtWork,
    GoingHome,
    GoingToService,
    Idle
}

public class Citizen
{
    public string id;
    public BuildingInstance homeBuilding;
    public BuildingInstance workBuilding;
    public Vehicle currentVehicle;
    public Vector3 position;
    public List<Vector2Int> currentPath;
    public int pathIndex;
    public CitizenState state;

    public Citizen(string id, BuildingInstance home)
    {
        this.id = id;
        this.homeBuilding = home;
        this.position = home.Position;
        this.state = CitizenState.AtHome;
        this.currentPath = new List<Vector2Int>();
        this.pathIndex = 0;
    }

    public void SetPath(List<Vector2Int> path)
    {
        currentPath = path ?? new List<Vector2Int>();
        pathIndex = 0;
    }

    public Vector3 GetNextWaypoint()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
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
            pathIndex = currentPath.Count;
        }
    }

    public bool HasArrived()
    {
        return pathIndex >= currentPath.Count;
    }
}
