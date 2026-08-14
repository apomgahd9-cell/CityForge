using System;
using System.Collections.Generic;

[Serializable]
public class GrowthProfileWrapper
{
    public Dictionary<string, GrowthProfile> profiles;
}

[Serializable]
public class GrowthProfile
{
    public string displayName;
    public List<string> tags;
    public List<GrowthStage> stages;
}

[Serializable]
public class GrowthStage
{
    public int level;
    public string label;
    public string upgradeTarget;
    public string downgradeTarget;
    public List<GrowthRule> rules;
    public string growthModel;
    public List<GrowthRule> declineRules;
    public string declineModel;
}

[Serializable]
public class GrowthRule
{
    public string type;
    public string serviceId;
    public string metric;
    public float value;
}

[Serializable]
public class GrowthModelWrapper
{
    public Dictionary<string, GrowthModel> models;
}

[Serializable]
public class GrowthModel
{
    public float baseChance;
    public List<GrowthModifier> modifiers;
    public float minChance;
    public float maxChance;
}

[Serializable]
public class GrowthModifier
{
    public string metric;
    public float weight;
    public float[] contributionRange;
}
