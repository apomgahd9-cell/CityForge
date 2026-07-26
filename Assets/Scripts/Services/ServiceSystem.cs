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
        Debug.Log($"Service added: {def.name} (id: {serviceId}, count: {activeServices[serviceId]})");
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
        Debug.Log($"Service removed: {serviceLookup[serviceId].name} (id: {serviceId}, count: {activeServices[serviceId]})");
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
        foreach (KeyValuePair<string, int> kvp in activeServices)
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
            if (!string.IsNullOrEmpty(def.effect.metric))
                MetricsSystem.Instance.SetMetric(def.effect.metric, 0);
        }

        foreach (KeyValuePair<string, int> kvp in activeServices)
        {
            if (kvp.Value <= 0) continue;
            if (!serviceLookup.TryGetValue(kvp.Key, out ServiceDefinition def)) continue;

            float currentVal = MetricsSystem.Instance.GetMetric(def.effect.metric);
            float newVal = currentVal;

            switch (def.effect.operation)
            {
                case "add":
                    newVal += def.effect.value * kvp.Value;
                    break;
                case "multiply":
                    newVal *= 1f + (def.effect.value * kvp.Value / 100f);
                    break;
                case "set":
                    newVal = def.effect.value;
                    break;
                default:
                    Debug.LogWarning($"Unknown operation '{def.effect.operation}' for service {def.id}");
                    break;
            }

            MetricsSystem.Instance.SetMetric(def.effect.metric, newVal);
        }
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

        List<string> keys = new List<string>(activeServices.Keys);
        foreach (string key in keys)
            activeServices[key] = 0;

        foreach (KeyValuePair<string, int> pair in data.activeServices)
        {
            if (serviceLookup.ContainsKey(pair.Key))
                activeServices[pair.Key] = pair.Value;
            else
                Debug.LogWarning($"Unknown service '{pair.Key}' in save file ignored.");
        }

        ApplyServiceEffects();
    }
}
