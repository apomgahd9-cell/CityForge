using UnityEngine;

public class CitizenSpawner : MonoBehaviour
{
    public static CitizenSpawner Instance { get; private set; }

    private int nextCitizenId;

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

    public void SpawnCitizensForBuilding(BuildingInstance building)
    {
        if (building == null) return;

        BuildingDefinition def = building.Definition;
        if (def.outputs == null || def.outputs.population == null) return;

        int populationMin = def.outputs.population.min;
        int populationMax = def.outputs.population.max;
        int citizenCount = Random.Range(populationMin, populationMax + 1);

        for (int i = 0; i < citizenCount; i++)
        {
            string citizenId = $"citizen_{nextCitizenId++}";
            Citizen citizen = new Citizen(citizenId, building);
            CitizenController.Instance?.RegisterCitizen(citizen);
            Debug.Log($"Citizen spawned: {citizenId} in {def.displayName}");
        }
    }

    public void DespawnCitizensForBuilding(BuildingInstance building)
    {
        if (building == null || CitizenController.Instance == null) return;

        // TODO: إزالة المواطنين المرتبطين بهذا المبنى
        Debug.Log($"Citizens despawned for building: {building.Definition.displayName}");
    }
}
