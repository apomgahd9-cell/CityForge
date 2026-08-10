using System;
using System.Collections.Generic;

[Serializable]
public class PolicyData
{
    public TaxPolicies taxPolicies;
}

[Serializable]
public class TaxPolicies
{
    public TaxPolicyItem residential;
    public TaxPolicyItem commercial;
    public TaxPolicyItem industrial;
}

[Serializable]
public class TaxPolicyItem
{
    public string id;
    public string displayName;
    public float defaultRate;
    public float minRate;
    public float maxRate;
    public float rateStep;
    public float currentRate;
}
