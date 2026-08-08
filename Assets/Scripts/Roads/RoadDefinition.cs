using System;

[Serializable]
public class RoadDefinition
{
    public string id;
    public string displayName;
    public float costPerTile;
    public float upkeepPerTile;
    public int speed;
    public int lanes;

    public int capacity
    {
        get { return lanes * 6; }
    }
}
