using UnityEngine;

public class RCISystem : MonoBehaviour
{
    public static RCISystem Instance { get; private set; }

    private RCIData rciData;
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
        rciData = JsonLoader.Load<RCIData>("Data/Balance/rci");
        if (rciData == null)
        {
            Debug.LogError("rci.json not found. RCI system disabled.");
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
        if (tick % 5 == 0)
        {
            CalculateAllDemands();
        }
    }

    private void CalculateAllDemands()
    {
        if (MetricsSystem.Instance == null) return;

        CalculateDemand("residential_demand");
        CalculateDemand("commercial_demand");
        CalculateDemand("industrial_demand");

        Debug.Log($"RCI: R={MetricsSystem.Instance.GetMetric("residential_demand"):F1}, " +
                  $"C={MetricsSystem.Instance.GetMetric("commercial_demand"):F1}, " +
                  $"I={MetricsSystem.Instance.GetMetric("industrial_demand"):F1}");
    }

    private void CalculateDemand(string demandKey)
    {
        DemandItem demandItem = GetDemandItem(demandKey);
        if (demandItem == null) return;

        float demand = demandItem.baseValue;

        foreach (var modifier in demandItem.modifiers)
        {
            float contribution = 0f;

            switch (modifier.function)
            {
                case "ratio":
                    float inputA = MetricsSystem.Instance.GetMetric(modifier.inputA);
                    float inputB = MetricsSystem.Instance.GetMetric(modifier.inputB);
                    if (inputB > 0)
                        contribution = (inputA / inputB) * modifier.weight;
                    break;

                case "linear":
                    float metricVal = MetricsSystem.Instance.GetMetric(modifier.metric);
                    float threshold = modifier.threshold;
                    contribution = (metricVal - threshold) * modifier.weight;
                    break;
            }

            demand += contribution;
        }

        demand = ApplyZoneModifier(demandKey, demand);
        demand = Mathf.Clamp(demand, demandItem.clampRange[0], demandItem.clampRange[1]);

        MetricsSystem.Instance.SetMetric(demandKey, demand);
    }

    private float ApplyZoneModifier(string demandKey, float currentDemand)
    {
        if (ZoneSystem.Instance == null) return currentDemand;

        ZoneType zoneType = demandKey switch
        {
            "residential_demand" => ZoneType.Residential,
            "commercial_demand" => ZoneType.Commercial,
            "industrial_demand" => ZoneType.Industrial,
            _ => (ZoneType)(-1)
        };

        if ((int)zoneType == -1) return currentDemand;

        int totalZones = ZoneSystem.Instance.GetZonesByType(zoneType).Count;
        int emptyZones = ZoneSystem.Instance.GetBuildableZones(zoneType).Count;

        if (totalZones > 0)
        {
            float fillRatio = (float)emptyZones / totalZones;
            currentDemand -= fillRatio * 15f;
        }
        else
        {
            currentDemand += 10f;
        }

        return currentDemand;
    }

    private DemandItem GetDemandItem(string demandKey)
    {
        return demandKey switch
        {
            "residential_demand" => rciData.demandCalculations.residential_demand,
            "commercial_demand" => rciData.demandCalculations.commercial_demand,
            "industrial_demand" => rciData.demandCalculations.industrial_demand,
            _ => null
        };
    }
}
