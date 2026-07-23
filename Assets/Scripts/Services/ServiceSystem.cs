using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// يدير الخدمات النشطة في المدينة. يحسب تأثيرها على المقاييس (Metrics) ويوفر
/// دوال استعلام للأنظمة الأخرى (النمو، الاقتصاد).
/// حاليًا لا يعتمد على مواقع، بل على تعداد الخدمات النشطة فقط.
/// </summary>
public class ServiceSystem : MonoBehaviour
{
    public static ServiceSystem Instance { get; private set; }

    private ServicesData servicesData;
    private SimulationClock clock;

    // عدد المباني النشطة لكل خدمة (المفتاح = serviceId مثل "water_tower")
    private Dictionary<string, int> activeServices = new Dictionary<string, int>();

    // قاموس للوصول السريع إلى تعريف الخدمة بواسطة id
    private Dictionary<string, ServiceDefinition> serviceLookup = new Dictionary<string, ServiceDefinition>();

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
        servicesData = JsonLoader.Load<ServicesData>("Data/Services/services");
        if (servicesData == null || servicesData.services == null)
        {
            Debug.LogError("services.json not found or empty! Service system disabled.");
            enabled = false;
            return;
        }

        // بناء القاموس السريع وتسجيل كل خدمة بقيمة 0
        foreach (var def in servicesData.services.Values)
        {
            serviceLookup[def.id] = def;
            activeServices[def.id] = 0;
        }

        clock = FindObjectOfType<SimulationClock>();
        if (clock != null)
        {
            clock.OnTick += OnSimulationTick;
        }
        else
        {
            Debug.LogError("SimulationClock not found!");
        }
    }

    private void OnDestroy()
    {
        if (clock != null)
            clock.OnTick -= OnSimulationTick;
    }

    private void OnSimulationTick(int tick)
    {
        // تُحدّث المقاييس في كل Tick (يمكن جعلها شهرية لاحقاً)
        ApplyServiceEffects();
    }

    /// <summary>
    /// يُضيف مبنى خدمة واحد إلى المدينة باستخدام serviceId (مثلاً "water_tower").
    /// </summary>
    public void AddService(string serviceId)
    {
        if (!serviceLookup.TryGetValue(serviceId, out var def))
        {
            Debug.LogError($"Service definition '{serviceId}' not found.");
            return;
        }

        activeServices[serviceId] = activeServices.GetValueOrDefault(serviceId, 0) + 1;
        Debug.Log($"Service added: {def.name} (id: {serviceId}, count: {activeServices[serviceId]})");
        ApplyServiceEffects();
    }

    /// <summary>
    /// يُزيل مبنى خدمة من المدينة.
    /// </summary>
    public void RemoveService(string serviceId)
    {
        if (!activeServices.ContainsKey(serviceId) || activeServices[serviceId] <= 0)
        {
            Debug.LogWarning($"No active service with id '{serviceId}' to remove.");
            return;
        }

        activeServices[serviceId]--;
        Debug.Log($"Service removed: {serviceLookup[serviceId].name} (id: {serviceId}, count: {activeServices[serviceId]})");
        ApplyServiceEffects();
    }

    /// <summary>
    /// يتحقق مما إذا كانت الخدمة المحددة (بـ serviceId) متوفرة حاليًا.
    /// يُستخدم في GrowthSystem للتحقق من قاعدة service_available.
    /// </summary>
    public bool HasService(string serviceId)
    {
        return activeServices.TryGetValue(serviceId, out int count) && count > 0;
    }

    /// <summary>
    /// يُرجع قيمة التغطية الحالية لمقياس معين (مثلاً "water_coverage").
    /// </summary>
    public float GetCoverage(string metric)
    {
        return MetricsSystem.Instance != null ? MetricsSystem.Instance.GetMetric(metric) : 0f;
    }

    /// <summary>
    /// يُرجع إجمالي تكاليف الصيانة الشهرية لجميع الخدمات النشطة.
    /// </summary>
    public float GetTotalUpkeep()
    {
        float total = 0f;
        foreach (var kvp in activeServices)
        {
            if (kvp.Value <= 0) continue;
            if (serviceLookup.TryGetValue(kvp.Key, out var def))
            {
                total += def.upkeep * kvp.Value;
            }
        }
        return total;
    }

    /// <summary>
    /// يطبق تأثيرات جميع الخدمات النشطة على المقاييس.
    /// TODO: في المستقبل، يجب ألا يمسح مقاييس أنظمة أخرى (مثل السكان والوظائف).
    /// يمكن تحقيق ذلك بجعل MetricsSystem يدعم فئات متعددة (serviceMetrics, buildingMetrics...).
    /// حاليًا نكتفي بتصغير المقاييس المرتبطة بالخدمات فقط.
    /// </summary>
    private void ApplyServiceEffects()
    {
        if (MetricsSystem.Instance == null) return;

        // تصفير المقاييس المرتبطة بالخدمات (نعيد حسابها من الصفر في كل مرة)
        // ملاحظة: هذا يمسح أي تأثيرات أخرى على هذه المقاييس - سيُحسّن لاحقًا.
        foreach (var def in servicesData.services.Values)
        {
            if (!string.IsNullOrEmpty(def.effect.metric))
            {
                MetricsSystem.Instance.SetMetric(def.effect.metric, 0);
            }
        }

        // تجميع التأثيرات من الخدمات النشطة
        foreach (var kvp in activeServices)
        {
            string id = kvp.Key;
            int count = kvp.Value;
            if (count <= 0) continue;

            if (!serviceLookup.TryGetValue(id, out var def)) continue;

            float currentVal = MetricsSystem.Instance.GetMetric(def.effect.metric);
            float newVal = currentVal;
            switch (def.effect.operation)
            {
                case "add":
                    newVal += def.effect.value * count;
                    break;
                case "multiply":
                    newVal *= (1f + def.effect.value * count / 100f);
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
}
