using System;
using Newtonsoft.Json;

[Serializable]
public class MetricDefinition
{
    public string id;
    public string displayName;
    public string type;
    public MetricRange range;

    [JsonProperty("default")]
    public float defaultValue;

    public string description;
}

[Serializable]
public class MetricRange
{
    public float min;
    public float max;
}
