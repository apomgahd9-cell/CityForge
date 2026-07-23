using System;
using System.Collections.Generic;

[Serializable]
public class ServicesData
{
    public int schemaVersion;
    public int version;
    public string description;
    public Dictionary<string, ServiceDefinition> services;
}

[Serializable]
public class ServiceDefinition
{
    public string id;
    public string name;
    public string type;
    public float cost;
    public float upkeep;
    public int radius;
    public string coverageType; // "radius" أو "network"
    public int capacity;
    public ServiceEffect effect;
}

[Serializable]
public class ServiceEffect
{
    public string metric;
    public string operation; // "add", "multiply", "set"
    public float value;
}
