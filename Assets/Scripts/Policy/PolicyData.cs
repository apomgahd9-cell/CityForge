using System;

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
