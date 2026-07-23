using System;

[Serializable]
public class GameRulesData
{
    public SimulationRules simulation;
}

[Serializable]
public class SimulationRules
{
    public int ticksPerGameDay;
    public int gameDaysPerMonth;
}
