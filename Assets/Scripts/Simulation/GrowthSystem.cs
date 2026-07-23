using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// يدير نمو المباني وتدهورها بناءً على ملفات growth_profiles و growth_models.
/// يعتمد على Newtonsoft.Json لتحميل القواميس من ملفات JSON.
/// </summary>
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
        growthModelData = JsonLoader.Load<GrowthModelWrapper>("Data/Balance/growth_models");

        if (growthProfileData == null || growthModelData == null)
        {
            Debug.LogError("Growth data files missing. Growth system disabled.");
            enabled = false;
            return;
        }

        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
        {
            clock.OnTick += OnSimulationTick;
        }
        else
        {
            Debug.LogError("SimulationClock not found.");
        }
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
        if (BuildingSpawner.Instance == null || MetricsSystem.Instance == null)
            return;

        // قائمة مؤقتة لتجنب التعديل على المجموعة أثناء السير عليها
        List<BuildingInstance> buildingsToRemove = new List<BuildingInstance>();

        foreach (var building in BuildingSpawner.Instance.GetAllBuildings())
        {
            var profile = FindProfile(building.Definition.zoneTags);
            if (profile == null) continue;

            var currentStage = profile.stages.Find(s => s.level == building.CurrentLevel);
            if (currentStage == null) continue;

            // تحقق من شروط النمو
            if (CheckRules(currentStage.rules))
            {
                float chance = CalculateChance(currentStage.growthModel);
                if (Random.value < chance)
                {
                    int nextLevel = building.CurrentLevel + 1;
                    var nextStage = profile.stages.Find(s => s.level == nextLevel);
                    if (nextStage != null)
                    {
                        building.SetLevel(nextLevel);
                        MetricsSystem.Instance.RecalculateAll();
                        Debug.Log($"{building.Definition.displayName} upgraded to Level {nextLevel} (Chance: {chance:P})");
                    }
                }
            }

            // تحقق من شروط التراجع
            if (currentStage.declineRules != null && CheckRules(currentStage.declineRules))
            {
                float declineChance = CalculateChance(currentStage.declineModel);
                if (Random.value < declineChance)
                {
                    int previousLevel = building.CurrentLevel - 1;
                    if (previousLevel >= 1)
                    {
                        building.SetLevel(previousLevel);
                        MetricsSystem.Instance.RecalculateAll();
                        Debug.Log($"{building.Definition.displayName} declined to Level {previousLevel}");
                    }
                    else
                    {
                        buildingsToRemove.Add(building);
                    }
                }
            }
        }

        // تنفيذ الحذف بعد انتهاء الحلقة بالكامل
        foreach (var building in buildingsToRemove)
        {
            BuildingSpawner.Instance.RemoveBuilding(building);
            Debug.Log($"{building.Definition.displayName} abandoned and removed.");
        }
    }

    private GrowthStage FindProfile(List<string> tags)
    {
        foreach (var profile in growthProfileData.profiles.Values)
        {
            if (profile.tags != null && profile.tags.Intersect(tags).Any())
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
                    // TODO: ربط مع نظام الخدمات لاحقاً
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
