using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 1;
    public string saveName;
    public long saveTimestamp;
    public float currentFunds;
    public float outstandingLoan;
    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<RoadSaveData> roads = new List<RoadSaveData>();
    public Dictionary<string, int> activeServices = new Dictionary<string, int>();
}
