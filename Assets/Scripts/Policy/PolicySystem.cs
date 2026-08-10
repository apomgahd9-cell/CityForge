using UnityEngine;

public class PolicySystem : MonoBehaviour, ISaveable
{
    public static PolicySystem Instance { get; private set; }

    private PolicyData policyData;

    public int LoadPriority => 50;

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

        policyData = JsonLoader.Load<PolicyData>("Data/Balance/policies");

        if (policyData == null)
        {
            Debug.LogError("policies.json not found. Policy system disabled.");
            enabled = false;
            return;
        }

        ApplyAllPolicies();
        Debug.Log("PolicySystem initialized.");
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Unregister(this);
    }

    public float GetTaxRate(string taxId)
    {
        TaxPolicyItem policy = GetTaxPolicy(taxId);
        if (policy == null)
        {
            Debug.LogWarning($"Tax policy not found: {taxId}");
            return 0f;
        }

        return policy.currentRate;
    }

    public void SetTaxRate(string taxId, float newRate)
    {
        TaxPolicyItem policy = GetTaxPolicy(taxId);
        if (policy == null)
        {
            Debug.LogWarning($"Tax policy not found: {taxId}");
            return;
        }

        newRate = RoundToStep(newRate, policy.rateStep);
        newRate = Mathf.Clamp(newRate, policy.minRate, policy.maxRate);
        policy.currentRate = newRate;

        ApplyTaxToMetrics(policy);
        Debug.Log($"Tax rate for {taxId} set to {newRate}%");
    }

    private void ApplyAllPolicies()
    {
        if (policyData?.taxPolicies == null) return;

        ApplyTaxToMetrics(policyData.taxPolicies.residential);
        ApplyTaxToMetrics(policyData.taxPolicies.commercial);
        ApplyTaxToMetrics(policyData.taxPolicies.industrial);
    }

    private void ApplyTaxToMetrics(TaxPolicyItem policy)
    {
        if (MetricsSystem.Instance == null || policy == null) return;
        MetricsSystem.Instance.SetMetric(policy.id, policy.currentRate);
    }

    private TaxPolicyItem GetTaxPolicy(string taxId)
    {
        if (policyData?.taxPolicies == null) return null;

        return taxId switch
        {
            "tax_residential" => policyData.taxPolicies.residential,
            "tax_commercial" => policyData.taxPolicies.commercial,
            "tax_industrial" => policyData.taxPolicies.industrial,
            _ => null
        };
    }

    private float RoundToStep(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Round(value / step) * step;
    }

    public void Save(SaveData data)
    {
        if (policyData?.taxPolicies == null) return;

        data.taxResidential = policyData.taxPolicies.residential.currentRate;
        data.taxCommercial = policyData.taxPolicies.commercial.currentRate;
        data.taxIndustrial = policyData.taxPolicies.industrial.currentRate;
    }

    public void Load(SaveData data)
    {
        SetTaxRate("tax_residential", data.taxResidential);
        SetTaxRate("tax_commercial", data.taxCommercial);
        SetTaxRate("tax_industrial", data.taxIndustrial);
    }
}
