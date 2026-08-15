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
        // TODO: فصل المراحل لمنع بناء + ترقية + تراجع لنفس المبنى في Tick واحد
        ProcessGrowth();
        ProcessUpgrades();
        ProcessDowngrades();
    }

    // ========== البناء الجديد ==========

    private void ProcessGrowth()
    {
        if (PlacementSystem.Instance == null || MetricsSystem.Instance == null || ZoneSystem.Instance == null)
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

        var zone = buildableZones[Random.Range(0, buildableZones.Count)];

        GrowthProfile profile = FindProfile(zoneType);
        if (profile == null) return false;

        int growthLevel = zone.density;
        GrowthStage currentStage = profile.stages.Find(s => s.level == growthLevel);
        if (currentStage == null) return false;

        string buildingId = GetBuildingIdForZone(zoneType, growthLevel);
        if (string.IsNullOrEmpty(buildingId)) return false;

        if (DataRegistry.Instance == null) return false;

        BuildingDefinition def = DataRegistry.Instance.GetBuilding(buildingId);
        if (def == null) return false;

        if (!CheckRules(currentStage.rules)) return false;

        float chance = CalculateChance(currentStage.growthModel);
        if (Random.value < chance)
        {
            Vector3 worldPos = GridSystem.Instance.GridToWorld(zone.gridX, zone.gridY);

            // البناء يتم عبر PlacementSystem بدلاً من استدعاء Spawner مباشرة
            bool placed = PlacementSystem.Instance.PlaceBuilding(buildingId, worldPos);

            if (placed)
            {
                Debug.Log($"Auto-built {buildingId} at ({zone.gridX}, {zone.gridY}) [Level: {growthLevel}]");
                return true;
            }
        }

        return false;
    }

    // ========== ترقية المباني الحالية ==========

    private void ProcessUpgrades()
    {
        if (BuildingSpawner.Instance == null || DataRegistry.Instance == null) return;

        var buildings = new List<BuildingInstance>(BuildingSpawner.Instance.GetAllBuildings());
        foreach (var building in buildings)
        {
            TryUpgradeBuilding(building);
        }
    }

    private void TryUpgradeBuilding(BuildingInstance building)
    {
        GrowthProfile profile = FindProfileForBuilding(building);
        if (profile == null) return;

        GrowthStage currentStage = profile.stages.Find(s => s.level == building.CurrentLevel);
        if (currentStage == null) return;

        if (string.IsNullOrEmpty(currentStage.upgradeTarget) || currentStage.upgradeTarget != "next")
            return;

        int nextLevel = building.CurrentLevel + 1;
        GrowthStage nextStage = profile.stages.Find(s => s.level == nextLevel);
        if (nextStage == null) return;

        if (!CheckRules(nextStage.rules)) return;

        float chance = CalculateChance(nextStage.growthModel);
        if (Random.value < chance)
        {
            string tag = GetTagForBuilding(building);
            if (string.IsNullOrEmpty(tag)) return;

            BuildingDefinition newDef = DataRegistry.Instance.GetRandomBuildingByTagAndLevel(tag, nextLevel);
            if (newDef == null) return;

            string oldName = building.Definition.displayName;
            bool success = BuildingSpawner.Instance.ReplaceBuilding(building, newDef.id);

            if (success)
            {
                Debug.Log($"{oldName} upgraded to {building.Definition.displayName} (Level {nextLevel})");
            }
        }
    }

    // ========== تراجع المباني ==========

    private void ProcessDowngrades()
    {
        if (BuildingSpawner.Instance == null || DataRegistry.Instance == null) return;

        var toRemove = new List<BuildingInstance>();

        var buildings = new List<BuildingInstance>(BuildingSpawner.Instance.GetAllBuildings());
        foreach (var building in buildings)
        {
            bool removed = TryDowngradeBuilding(building);
            if (removed)
                toRemove.Add(building);
        }

        foreach (var building in toRemove)
        {
            if (GridSystem.Instance != null)
            {
                BuildingDefinition def = building.Definition;
                int areaWidth = def.size != null ? def.size.width : 1;
                int areaDepth = def.size != null ? def.size.depth : 1;

                if (GridSystem.Instance.WorldToGrid(building.Position, out int gridX, out int gridY))
                {
                    for (int x = gridX; x < gridX + areaWidth; x++)
                    {
                        for (int y = gridY; y < gridY + areaDepth; y++)
                        {
                            GridSystem.Instance.SetTile(x, y, new TileData { gridX = x, gridY = y, type = TileType.Empty });
                        }
                    }
                }
            }

            if (OccupancyMap.Instance != null && building.OccupantId > 0)
                OccupancyMap.Instance.FreeAreaByOccupantId(building.OccupantId);

            BuildingSpawner.Instance.RemoveBuilding(building);
            Debug.Log($"{building.Definition.displayName} abandoned and removed.");
        }
    }

    private bool TryDowngradeBuilding(BuildingInstance building)
    {
        GrowthProfile profile = FindProfileForBuilding(building);
        if (profile == null) return false;

        GrowthStage currentStage = profile.stages.Find(s => s.level == building.CurrentLevel);
        if (currentStage == null) return false;

        if (currentStage.declineRules == null || currentStage.declineRules.Count == 0)
            return false;

        // TODO: مراجعة منطق declineRules مقابل تصميم ملفات JSON
        if (!CheckRules(currentStage.declineRules)) return false;

        float chance = CalculateChance(currentStage.declineModel);
        if (Random.value < chance)
        {
            int previousLevel = building.CurrentLevel - 1;

            if (previousLevel >= 1)
            {
                string tag = GetTagForBuilding(building);
                if (string.IsNullOrEmpty(tag)) return false;

                BuildingDefinition newDef = DataRegistry.Instance.GetRandomBuildingByTagAndLevel(tag, previousLevel);
                if (newDef == null) return false;

                string oldName = building.Definition.displayName;
                bool success = BuildingSpawner.Instance.ReplaceBuilding(building, newDef.id);

                if (success)
                {
                    Debug.Log($"{oldName} downgraded to {building.Definition.displayName} (Level {previousLevel})");
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    // ========== دوال مساعدة ==========

    private string GetBuildingIdForZone(ZoneType zoneType, int level)
    {
        if (DataRegistry.Instance == null) return null;

        BuildingDefinition def = null;

        if (BuildingCategoryMapper.Instance != null)
        {
            def = BuildingCategoryMapper.Instance.GetBuildingForZone(zoneType, level);
        }

        if (def == null)
        {
            string tag = zoneType switch
            {
                ZoneType.Residential => "residential",
                ZoneType.Commercial  => "commercial",
                ZoneType.Industrial  => "industrial",
                _ => null
            };
            def = DataRegistry.Instance.GetRandomBuildingByTagAndLevel(tag, level);
        }

        return def?.id;
    }

    private string GetTagForBuilding(BuildingInstance building)
    {
        if (building.Definition.zoneTags == null) return null;

        string[] knownTags = { "residential", "commercial", "industrial" };
        foreach (string tag in knownTags)
        {
            if (building.Definition.zoneTags.Contains(tag))
                return tag;
        }
        return null;
    }

    private GrowthProfile FindProfile(ZoneType zoneType)
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

    private GrowthProfile FindProfileForBuilding(BuildingInstance building)
    {
        if (building.Definition.zoneTags == null) return null;

        foreach (var profile in growthProfileData.profiles.Values)
        {
            if (profile.tags == null) continue;

            foreach (var tag in building.Definition.zoneTags)
            {
                if (profile.tags.Contains(tag))
                    return profile;
            }
        }
        return null;
    }

    private bool CheckRules(List<GrowthRule> rules)
    {
        if (rules == null) return true;

        foreach (var rule in rules)
        {
            if (rule == null) continue;

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
                    if (MetricsSystem.Instance == null)
                    {
                        Debug.LogWarning("MetricsSystem is not available for growth check.");
                        return false;
                    }

                    if (MetricsSystem.Instance.GetMetric(rule.metric) < rule.value)
                        return false;

                    break;

                case "metric_max":
                    if (MetricsSystem.Instance == null)
                    {
                        Debug.LogWarning("MetricsSystem is not available for growth check.");
                        return false;
                    }

                    if (MetricsSystem.Instance.GetMetric(rule.metric) > rule.value)
                        return false;

                    break;
            }
        }

        return true;
    }

    private float CalculateChance(string modelId)
    {
        if (growthModelData == null || growthModelData.models == null)
            return 0f;

        if (!growthModelData.models.TryGetValue(modelId, out var model))
            return 0f;

        if (model == null)
            return 0f;

        float chance = model.baseChance;

        if (MetricsSystem.Instance != null && model.modifiers != null)
        {
            foreach (var mod in model.modifiers)
            {
                if (mod == null) continue;
                if (mod.contributionRange == null || mod.contributionRange.Length < 2) continue;

                float metricVal = MetricsSystem.Instance.GetMetric(mod.metric);
                float contribution = metricVal * mod.weight;
                contribution = Mathf.Clamp(contribution, mod.contributionRange[0], mod.contributionRange[1]);
                chance += contribution;
            }
        }

        return Mathf.Clamp(chance, model.minChance, model.maxChance);
    }

    private TileType GetTileTypeForZone(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Residential => TileType.Residential,
            ZoneType.Commercial  => TileType.Commercial,
            ZoneType.Industrial  => TileType.Industrial,
            _ => TileType.Empty
        };
    }
}
