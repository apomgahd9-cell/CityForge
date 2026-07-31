using System;
using System.Collections.Generic;

[Serializable]
public class RCIData
{
    public int schemaVersion;
    public int version;
    public string description;
    public DemandCalculations demandCalculations;
}

[Serializable]
public class DemandCalculations
{
    public DemandItem residential_demand;
    public DemandItem commercial_demand;
    public DemandItem industrial_demand;
}

[Serializable]
public class DemandItem
{
    public string description;
    public float baseValue;
    public List<DemandModifier> modifiers;
    public float[] clampRange;
}

[Serializable]
public class DemandModifier
{
    public string function;
    public string inputA;
    public string inputB;
    public string metric;
    public float weight;
    public float threshold;
    public string description;
}
