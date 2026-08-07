using System.Collections.Generic;
using UnityEngine;

public class CitizenController : MonoBehaviour
{
    public static CitizenController Instance { get; private set; }

    private List<Citizen> citizens = new List<Citizen>();

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

    public void RegisterCitizen(Citizen citizen)
    {
        if (citizen == null) return;
        if (!citizens.Contains(citizen))
            citizens.Add(citizen);
    }

    public void UnregisterCitizen(Citizen citizen)
    {
        if (citizen == null) return;
        citizens.Remove(citizen);
    }

    public bool RequestPath(Citizen citizen, Vector3 targetPosition)
    {
        if (GridSystem.Instance == null || PathfindingSystem.Instance == null)
            return false;

        if (!GridSystem.Instance.WorldToGrid(citizen.position, out int startX, out int startY))
            return false;

        if (!GridSystem.Instance.WorldToGrid(targetPosition, out int targetX, out int targetY))
            return false;

        Vector2Int start = new Vector2Int(startX, startY);
        Vector2Int target = new Vector2Int(targetX, targetY);

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(start, target);
        if (path == null || path.Count == 0)
            return false;

        citizen.SetPath(path);
        return true;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        float walkSpeed = 5f;

        for (int i = citizens.Count - 1; i >= 0; i--)
        {
            Citizen citizen = citizens[i];
            if (citizen.state != CitizenState.GoingToWork &&
                citizen.state != CitizenState.GoingHome &&
                citizen.state != CitizenState.GoingToService)
                continue;

            Vector3 targetWaypoint = citizen.GetNextWaypoint();
            Vector3 direction = targetWaypoint - citizen.position;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                citizen.position = targetWaypoint;
                citizen.AdvanceToNextWaypoint();

                if (citizen.HasArrived())
                {
                    switch (citizen.state)
                    {
                        case CitizenState.GoingToWork:
                            citizen.state = CitizenState.AtWork;
                            if (citizen.workBuilding != null)
                                citizen.position = citizen.workBuilding.Position;
                            break;
                        case CitizenState.GoingHome:
                            citizen.state = CitizenState.AtHome;
                            if (citizen.homeBuilding != null)
                                citizen.position = citizen.homeBuilding.Position;
                            break;
                        case CitizenState.GoingToService:
                            citizen.state = CitizenState.Idle;
                            break;
                    }
                }
            }
            else
            {
                float step = walkSpeed * deltaTime;
                if (step >= distance)
                {
                    citizen.position = targetWaypoint;
                    citizen.AdvanceToNextWaypoint();
                }
                else
                {
                    citizen.position += direction.normalized * step;
                }
            }
        }
    }
}
