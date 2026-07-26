using System;

[Serializable]
public struct TileData
{
    public int gridX;
    public int gridY;
    public TileType type;
}

[Serializable]
public enum TileType
{
    Empty,
    Residential,
    Commercial,
    Industrial,
    Road,
    Service
}
