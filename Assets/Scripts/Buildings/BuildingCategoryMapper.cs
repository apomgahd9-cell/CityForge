using System.Collections.Generic;
using UnityEngine;

public class BuildingCategoryMapper : MonoBehaviour
{
    public static BuildingCategoryMapper Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GetCategoryForDensity(ZoneType zoneType, int density)
    {
        return zoneType switch
        {
            ZoneType.Residential => density switch
            {
                1 => "house",
                2 => "apartment",
                _ => "estate"
            },
            ZoneType.Commercial => density switch
            {
                1 => "shop",
                2 => "mall",
                _ => "office"
            },
            ZoneType.Industrial => density switch
            {
                1 => "factory",
                2 => "warehouse",
                _ => "refinery"
            },
            _ => null
        };
    }

    public BuildingDefinition GetBuildingForZone(ZoneType zoneType, int density)
    {
        if (DataRegistry.Instance == null) return null;

        string category = GetCategoryForDensity(zoneType, density);
        string tag = GetTagForZoneType(zoneType);

        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(tag))
            return null;

        var matches = DataRegistry.Instance.GetBuildingsByTagAndCategory(tag, category);
        if (matches != null && matches.Count > 0)
            return matches[Random.Range(0, matches.Count)];

        return DataRegistry.Instance.GetRandomBuildingByTagAndLevel(tag, density);
    }

    private string GetTagForZoneType(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Residential => "residential",
            ZoneType.Commercial => "commercial",
            ZoneType.Industrial => "industrial",
            _ => null
        };
    }
}
