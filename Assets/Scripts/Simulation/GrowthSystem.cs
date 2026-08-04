using System.Collections.Generic;
using UnityEngine;

public class GrowthSystem : MonoBehaviour
{
    public static GrowthSystem Instance { get; private set; }

    private GrowthProfileWrapper growthProfileData;
    private GrowthModelWrapper growthModelData;
    private SimulationClock clock;

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

    private void Start()
    {
        growthProfileData = JsonLoader.Load<GrowthProfileWrapper>("Data/Balance/growth_profiles");
        growthModelData   = JsonLoader.Load<GrowthModelWrapper>("Data/Balance/growth_models");

        if (growthProfileData == null || growthModelData == null)
        {
            Debug.LogError("Growth data files missing. Growth system disabled.");
            enabled = false;
            return;
        }

        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
            clock.OnTick += OnSimulationTick;
        else
            Debug.LogError("SimulationClock not found.");
    }

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnTick -= OnSimulationTick;
    }

    private void OnSimulationTick(int tick)
    {
        ProcessGrowth();
    }

    private void ProcessGrowth()
    {
        if (BuildingSpawner.Instance == null || MetricsSystem.Instance == null || ZoneSystem.Instance == null)
            return;

        float residentialDemand = MetricsSystem.Instance.GetMetric("residential_demand");
        float commercialDemand  = MetricsSystem.Instance.GetMetric("commercial_demand");
        float industrialDemand  = MetricsSystem.Instance.GetMetric("industrial_demand");

        var zoneOrder = new List<(ZoneType type, float demand)>
        {
            (ZoneType.Residential, residentialDemand),
            (ZoneType.Commercial,  commercialDemand),
            (ZoneType.Industrial,  industrialDemand)
        };
        zoneOrder.Sort((a, b) => b.demand.CompareTo(a.demand));

        foreach (var (zoneType, demand) in zoneOrder)
        {
            if (demand <= 0) continue;

            if (TryBuildInZone(zoneType))
                break;
        }
    }

    private bool TryBuildInZone(ZoneType zoneType)
    {
        var buildableZones = ZoneSystem.Instance.GetBuildableZones(zoneType);
        if (buildableZones.Count == 0) return false;

        string buildingId = GetBuildingIdForZone(zoneType);
        if (string.IsNullOrEmpty(buildingId)) return false;

        var profile = FindProfile(zoneType);
        if (profile == null) return false;

        // TODO: دعم مستويات النمو الأعلى (Level 2, 3) بدلاً من Level 1 فقط
        var currentStage = profile.stages.Find(s => s.level == 1);
        if (currentStage == null) return false;

        var zone = buildableZones[Random.Range(0, buildableZones.Count)];

        if (!CheckRules(currentStage.rules)) return false;

        float chance = CalculateChance(currentStage.growthModel);
        if (Random.value < chance)
        {
            Vector3 worldPos = GridSystem.Instance.GridToWorld(zone.gridX, zone.gridY);

            BuildingInstance instance = BuildingSpawner.Instance.SpawnBuilding(buildingId, worldPos);
            if (instance != null)
            {
                if (OccupancyMap.Instance != null)
                {
                    int occupantId = OccupancyMap.Instance.OccupyArea(zone.gridX, zone.gridY, 1, 1);
                    instance.SetOccupantId(occupantId);
                }

                Debug.Log($"Auto-built {buildingId} at ({zone.gridX}, {zone.gridY})");
                return true;
            }
        }

        return false;
    }

    private string GetBuildingIdForZone(ZoneType zoneType)
    {
        if (DataRegistry.Instance == null)
        {
            Debug.LogWarning("DataRegistry not available.");
            return null;
        }

        string tag = zoneType switch
        {
            ZoneType.Residential => "residential",
            ZoneType.Commercial  => "commercial",
            ZoneType.Industrial  => "industrial",
            _ => null
        };

        if (string.IsNullOrEmpty(tag)) return null;

        // TODO: دعم مستويات أعلى من 1 لاحقاً
        BuildingDefinition def = DataRegistry.Instance.GetRandomBuildingByTagAndLevel(tag, 1);
        return def?.id;
    }

    private GrowthStage FindProfile(ZoneType zoneType)
    {
        string tag = zoneType switch
        {
            ZoneType.Residential => "residential",
            ZoneType.Commercial  => "commercial",
            ZoneType.Industrial  => "industrial",
            _ => ""
        };

        foreach (var profile in growthProfileData.profiles.Values)
        {
            if (profile.tags != null && profile.tags.Contains(tag))
                return profile;
        }
        return null;
    }

    private bool CheckRules(List<GrowthRule> rules)
    {
        if (rules == null) return true;
        foreach (var rule in rules)
        {
            switch (rule.type)
            {
                case "service_available":
                    if (string.IsNullOrWhiteSpace(rule.serviceId))
                    {
                        Debug.LogWarning("Growth rule 'service_available' is missing serviceId.");
                        return false;
                    }
                    if (ServiceSystem.Instance == null)
                    {
                        Debug.LogWarning("ServiceSystem is not available for growth check.");
                        return false;
                    }
                    if (!ServiceSystem.Instance.HasService(rule.serviceId))
                        return false;
                    break;

                case "metric_min":
                    if (MetricsSystem.Instance.GetMetric(rule.metric) < rule.value)
                        return false;
                    break;

                case "metric_max":
                    if (MetricsSystem.Instance.GetMetric(rule.metric) > rule.value)
                        return false;
                    break;
            }
        }
        return true;
    }

    private float CalculateChance(string modelId)
    {
        if (!growthModelData.models.TryGetValue(modelId, out var model))
            return 0f;

        float chance = model.baseChance;
        foreach (var mod in model.modifiers)
        {
            float metricVal = MetricsSystem.Instance.GetMetric(mod.metric);
            float contribution = metricVal * mod.weight;
            contribution = Mathf.Clamp(contribution, mod.contributionRange[0], mod.contributionRange[1]);
            chance += contribution;
        }
        return Mathf.Clamp(chance, model.minChance, model.maxChance);
    }
}
