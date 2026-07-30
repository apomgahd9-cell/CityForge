using System;
using UnityEngine;

[Serializable]
public class ZoneData
{
    public int gridX;
    public int gridY;
    public ZoneType zoneType;
    public int density;          // 1 = Low, 2 = Medium, 3 = High
    public int maxBuildings;     // الحد الأقصى للمباني في هذه البلاطة
}

[Serializable]
public enum ZoneType
{
    Residential,
    Commercial,
    Industrial
}
