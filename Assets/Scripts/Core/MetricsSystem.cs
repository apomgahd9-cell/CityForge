using System.Collections.Generic;
using UnityEngine;

public class MetricsSystem : MonoBehaviour
{
    public static MetricsSystem Instance { get; private set; }

    private Dictionary<string, float> metrics = new();
    private List<BuildingInstance> buildings = new();

    private static readonly string[] buildingMetrics =
    {
        "population_total",
        "jobs_available",
        "workforce_available",
        "jobs_industrial",
        "customer_access",
        "freight_access",
        "pollution"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeMetrics();
    }

    private void Start()
    {
        SimulationClock clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
            clock.OnTick += UpdateMetrics;
    }

    private void InitializeMetrics()
    {
        MetricsData data = JsonLoader.Load<MetricsData>("Data/Balance/metrics");
        if (data != null && data.metrics != null)
        {
            foreach (var kvp in data.metrics)
            {
                metrics[kvp.Key] = kvp.Value.defaultValue;
            }
            Debug.Log($"MetricsSystem initialized from metrics.json ({metrics.Count} metrics)");
        }
        else
        {
            Debug.LogWarning("metrics.json not found. Using fallback values.");
            metrics["population_total"] = 0;
            metrics["jobs_available"] = 0;
            metrics["workforce_available"] = 0;
            metrics["jobs_industrial"] = 0;
            metrics["freight_access"] = 0;
            metrics["pollution"] = 0;
            metrics["customer_access"] = 0;
            metrics["water_coverage"] = 0;
            metrics["power_coverage"] = 0;
            metrics["crime_rate"] = 100;
            metrics["fire_risk"] = 0;
            metrics["health_coverage"] = 0;
            metrics["education_coverage"] = 0;
        }
    }

    public void AddBuilding(BuildingInstance instance)
    {
        if (!buildings.Contains(instance))
        {
            buildings.Add(instance);
            RecalculateAll();
        }
    }

    public void RemoveBuilding(BuildingInstance instance)
    {
        if (buildings.Contains(instance))
        {
            buildings.Remove(instance);
            RecalculateAll();
        }
    }

    private void UpdateMetrics(int tick)
    {
        RecalculateAll();
    }

    public void RecalculateAll()
    {
        ResetBuildingMetrics();
        foreach (var building in buildings)
            ApplyBuilding(building);
    }

    private void ResetBuildingMetrics()
    {
        foreach (var metric in buildingMetrics)
        {
            if (metrics.ContainsKey(metric))
                metrics[metric] = 0;
        }
    }

    private void ApplyBuilding(BuildingInstance instance)
    {
        var def = instance.Definition;

        metrics["population_total"] += instance.Population;
        metrics["jobs_available"] += instance.Jobs;
        metrics["customer_access"] += def.outputs.customer_access;
        metrics["freight_access"] += def.outputs.freight_access;
        metrics["pollution"] += def.outputs.pollution;
    }

    public float GetMetric(string id)
    {
        if (metrics.TryGetValue(id, out float value))
            return value;
        Debug.LogWarning($"Metric '{id}' not found.");
        return 0;
    }

    public void SetMetric(string id, float value)
    {
        if (metrics.ContainsKey(id))
            metrics[id] = value;
        else
            metrics.Add(id, value);
    }
}
