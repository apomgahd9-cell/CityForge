using System.Collections.Generic;
using UnityEngine;

public class ServiceSystem : MonoBehaviour, ISaveable
{
    public static ServiceSystem Instance { get; private set; }

    private ServicesData servicesData;
    private Dictionary<string, int> activeServices = new Dictionary<string, int>();
    private Dictionary<string, ServiceDefinition> serviceLookup = new Dictionary<string, ServiceDefinition>();

    public int LoadPriority => 100;

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
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Register(this);

        servicesData = JsonLoader.Load<ServicesData>("Data/Services/services");
        if (servicesData == null || servicesData.services == null)
        {
            Debug.LogError("services.json not found. Service system disabled.");
            enabled = false;
            return;
        }

        foreach (ServiceDefinition def in servicesData.services.Values)
        {
            serviceLookup[def.id] = def;
            activeServices[def.id] = 0;
        }

        ApplyServiceEffects();
        Debug.Log("ServiceSystem initialized.");
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Unregister(this);
    }

    public void AddService(string serviceId)
    {
        if (!serviceLookup.TryGetValue(serviceId, out ServiceDefinition def))
        {
            Debug.LogError($"Service definition not found: {serviceId}");
            return;
        }

        if (!activeServices.ContainsKey(serviceId))
            activeServices[serviceId] = 0;

        activeServices[serviceId]++;

        string serviceName = GetServiceName(def);
        Debug.Log($"Service added: {serviceName} (id: {serviceId}, count: {activeServices[serviceId]})");
        ApplyServiceEffects();
    }

    public void RemoveService(string serviceId)
    {
        if (!activeServices.TryGetValue(serviceId, out int count) || count <= 0)
        {
            Debug.LogWarning($"No active service with id: {serviceId}");
            return;
        }

        activeServices[serviceId]--;

        string serviceName = GetServiceName(serviceLookup[serviceId]);
        Debug.Log($"Service removed: {serviceName} (id: {serviceId}, count: {activeServices[serviceId]})");
        ApplyServiceEffects();
    }

    public bool HasService(string serviceId)
    {
        return activeServices.TryGetValue(serviceId, out int count) && count > 0;
    }

    public float GetCoverage(string metric)
    {
        return MetricsSystem.Instance != null ? MetricsSystem.Instance.GetMetric(metric) : 0f;
    }

    public float GetTotalUpkeep()
    {
        float total = 0f;
        foreach (var kvp in activeServices)
        {
            if (kvp.Value <= 0) continue;
            if (serviceLookup.TryGetValue(kvp.Key, out ServiceDefinition def))
                total += def.upkeep * kvp.Value;
        }
        return total;
    }

    public void RecalculateAllEffects()
    {
        ApplyServiceEffects();
    }

    private void ApplyServiceEffects()
    {
        if (servicesData == null || MetricsSystem.Instance == null) return;

        foreach (ServiceDefinition def in servicesData.services.Values)
        {
            string metric = GetMetricId(def);
            if (!string.IsNullOrEmpty(metric))
                MetricsSystem.Instance.SetMetric(metric, 0);
        }

        if (ServiceCoverageSystem.Instance != null && GridSystem.Instance != null)
        {
            int totalTiles = GridSystem.Instance.Width * GridSystem.Instance.Height;

            foreach (ServiceDefinition def in servicesData.services.Values)
            {
                int coveredCount = ServiceCoverageSystem.Instance.GetCoverage(def.id).Count;
                float coveragePercent = totalTiles > 0 ? (float)coveredCount / totalTiles * 100f : 0f;

                string metric = GetMetricId(def);
                if (!string.IsNullOrEmpty(metric))
                {
                    float current = MetricsSystem.Instance.GetMetric(metric);
                    MetricsSystem.Instance.SetMetric(metric, current + coveragePercent);
                }
            }
        }
    }

    private string GetMetricId(ServiceDefinition def)
    {
        if (def.effect != null && !string.IsNullOrEmpty(def.effect.metric))
            return def.effect.metric;

        if (!string.IsNullOrEmpty(def.metricId))
            return def.metricId;

        return null;
    }

    private string GetServiceName(ServiceDefinition def)
    {
        if (!string.IsNullOrEmpty(def.displayName))
            return def.displayName;

        if (!string.IsNullOrEmpty(def.name))
            return def.name;

        return def.id;
    }

    public void Save(SaveData data)
    {
        data.activeServices = new Dictionary<string, int>(activeServices);
    }

    public void Load(SaveData data)
    {
        if (serviceLookup == null || serviceLookup.Count == 0)
        {
            Debug.LogWarning("ServiceSystem.Load() called before definitions loaded.");
            return;
        }

        if (data.activeServices == null)
        {
            ApplyServiceEffects();
            return;
        }

        var keys = new List<string>(activeServices.Keys);
        foreach (string key in keys)
            activeServices[key] = 0;

        foreach (var pair in data.activeServices)
        {
            if (serviceLookup.ContainsKey(pair.Key))
                activeServices[pair.Key] = pair.Value;
            else
                Debug.LogWarning($"Unknown service '{pair.Key}' in save file ignored.");
        }

        ApplyServiceEffects();
    }
}
