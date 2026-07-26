using System.Collections.Generic;
using UnityEngine;

public class DataRegistry : MonoBehaviour
{
    public static DataRegistry Instance { get; private set; }

    public Dictionary<string, BuildingDefinition> Buildings { get; private set; } = new Dictionary<string, BuildingDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        LoadBuildings();
        ValidateAllData();
        Debug.Log("DataRegistry initialized.");
    }

    private void LoadBuildings()
    {
        BuildingListWrapper wrapper = JsonLoader.Load<BuildingListWrapper>("Data/Definitions/buildings");

        if (wrapper != null && wrapper.buildings != null)
        {
            foreach (BuildingDefinition building in wrapper.buildings)
            {
                if (!string.IsNullOrEmpty(building.id))
                {
                    Buildings[building.id] = building;
                    Debug.Log($"Building loaded: {building.id} - {building.displayName}");
                }
            }
        }
    }

    private void ValidateAllData()
    {
        ServicesData services = JsonLoader.Load<ServicesData>("Data/Services/services");
        EconomyData economy = JsonLoader.Load<EconomyData>("Data/Balance/economy");
        GrowthProfileWrapper profiles = JsonLoader.Load<GrowthProfileWrapper>("Data/Balance/growth_profiles");
        GrowthModelWrapper models = JsonLoader.Load<GrowthModelWrapper>("Data/Balance/growth_models");

        DataValidator.ValidateAll(services, economy, profiles, models);
    }

    public BuildingDefinition GetBuilding(string id)
    {
        Buildings.TryGetValue(id, out BuildingDefinition building);
        return building;
    }
}
