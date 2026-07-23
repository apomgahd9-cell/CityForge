using System;
using System.Collections.Generic;

[Serializable]
public class GrowthProfileWrapper
{
    public Dictionary<string, GrowthStage> profiles;
}

[Serializable]
public class GrowthStage
{
    public List<string> tags;
    public List<GrowthStageLevel> stages;
}

[Serializable]
public class GrowthStageLevel
{
    public int level;
    public string upgradeTarget;
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
