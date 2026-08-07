using System.Collections.Generic;
using UnityEngine;

public class JobAssignmentSystem : MonoBehaviour
{
    public static JobAssignmentSystem Instance { get; private set; }

    private List<Citizen> unemployedCitizens = new List<Citizen>();
    private List<BuildingInstance> buildingsWithJobs = new List<BuildingInstance>();

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

    public void RegisterUnemployedCitizen(Citizen citizen)
    {
        if (citizen == null || citizen.workBuilding != null) return;
        if (!unemployedCitizens.Contains(citizen))
        {
            unemployedCitizens.Add(citizen);
        }
    }

    public void RegisterBuildingWithJobs(BuildingInstance building)
    {
        if (building == null) return;
        BuildingDefinition def = building.Definition;
        if (def.outputs == null || def.outputs.jobs_available == null) return;

        if (!buildingsWithJobs.Contains(building))
        {
            buildingsWithJobs.Add(building);
        }
    }

    public void UnregisterBuilding(BuildingInstance building)
    {
        buildingsWithJobs.Remove(building);
    }

    public void AssignJobs()
    {
        for (int i = unemployedCitizens.Count - 1; i >= 0; i--)
        {
            Citizen citizen = unemployedCitizens[i];

            BuildingInstance workplace = FindAvailableJob();
            if (workplace == null) break;

            Vehicle createdVehicle = null;

            if (citizen.currentVehicle == null && VehicleSpawner.Instance != null)
            {
                createdVehicle = VehicleSpawner.Instance.SpawnVehicle(citizen.position);
                citizen.currentVehicle = createdVehicle;
            }

            if (CitizenController.Instance == null)
            {
                RollbackVehicle(citizen, createdVehicle);
                continue;
            }

            bool pathFound = CitizenController.Instance.RequestPath(citizen, workplace.Position);

            if (!pathFound)
            {
                RollbackVehicle(citizen, createdVehicle);
                continue;
            }

            citizen.workBuilding = workplace;
            unemployedCitizens.RemoveAt(i);
            citizen.state = CitizenState.GoingToWork;

            Debug.Log($"{citizen.id} assigned to {workplace.Definition.displayName}");
        }
    }

    private void RollbackVehicle(Citizen citizen, Vehicle createdVehicle)
    {
        if (createdVehicle != null)
        {
            VehicleSpawner.Instance?.DespawnVehicle(createdVehicle);
            citizen.currentVehicle = null;
        }
    }

    private BuildingInstance FindAvailableJob()
    {
        foreach (BuildingInstance building in buildingsWithJobs)
        {
            if (building.Definition.outputs.jobs_available == null) continue;

            int maxJobs = building.Definition.outputs.jobs_available.max;
            int assignedCount = CountAssignedWorkers(building);

            if (assignedCount < maxJobs)
                return building;
        }
        return null;
    }

    private int CountAssignedWorkers(BuildingInstance building)
    {
        if (CitizenController.Instance == null) return 0;

        int count = 0;
        foreach (Citizen citizen in CitizenController.Instance.GetAllCitizens())
        {
            if (citizen.workBuilding == building)
                count++;
        }
        return count;
    }
}
