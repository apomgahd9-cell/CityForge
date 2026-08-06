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
