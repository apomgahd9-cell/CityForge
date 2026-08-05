using System;
using System.Collections.Generic;

[Serializable]
public class MetricsData
{
    public int version;
    public string description;
    public Dictionary<string, MetricDefinition> metrics;
}
