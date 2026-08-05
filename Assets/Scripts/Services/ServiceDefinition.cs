using System;

[Serializable]
public class ServiceDefinition
{
    public string id;
    public string name;
    public string displayName;
    public string type;
    public float cost;
    public float upkeep;
    public int radius;
    public string coverageType;
    public int capacity;
    public string metricId;
    public ServiceEffect effect;
}

[Serializable]
public class ServiceEffect
{
    public string metric;
    public string operation;
    public float value;
}
