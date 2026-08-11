using System.Collections.Generic;
using UnityEngine;

public class DataRegistry : MonoBehaviour
{
    public static DataRegistry Instance { get; private set; }

    public Dictionary<string, BuildingDefinition> Buildings { get; private set; } = new Dictionary<string, BuildingDefinition>();
    public Dictionary<string, RoadDefinition> Roads { get; private set; } = new Dictionary<string, RoadDefinition>();

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
        LoadRoads();
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
                }
            }
        }
    }

    private void LoadRoads()
    {
        RoadListWrapper wrapper = JsonLoader.Load<RoadListWrapper>("Data/Definitions/roads");

        if (wrapper != null && wrapper.roads != null)
        {
            foreach (RoadDefinition road in wrapper.roads)
            {
                if (!string.IsNullOrEmpty(road.id))
                {
                    Roads[road.id] = road;
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

    public RoadDefinition GetRoad(string id)
    {
        Roads.TryGetValue(id, out RoadDefinition road);
        return road;
    }

    public List<BuildingDefinition> GetBuildingsByTagAndCategory(string tag, string category)
    {
        List<BuildingDefinition> matches = new List<BuildingDefinition>();

        foreach (BuildingDefinition building in Buildings.Values)
        {
            if (building == null) continue;
            if (building.category != category) continue;
            if (building.zoneTags == null || !building.zoneTags.Contains(tag)) continue;

            matches.Add(building);
        }

        return matches;
    }

    public BuildingDefinition GetRandomBuildingByTagAndLevel(string tag, int level)
    {
        List<BuildingDefinition> matches = new List<BuildingDefinition>();

        foreach (BuildingDefinition building in Buildings.Values)
        {
            if (building == null) continue;
            if (building.level != level) continue;
            if (building.zoneTags == null || !building.zoneTags.Contains(tag)) continue;

            matches.Add(building);
        }

        if (matches.Count == 0) return null;
        return matches[Random.Range(0, matches.Count)];
    }
}

[System.Serializable]
public class RoadListWrapper
{
    public List<RoadDefinition> roads;
}
