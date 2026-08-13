using UnityEngine;

public class PlacementValidator : MonoBehaviour
{
    public static PlacementValidator Instance { get; private set; }

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

    public bool Validate(BuildingDefinition def, Vector3 worldPosition, out int gridX, out int gridY, out string error)
    {
        gridX = 0;
        gridY = 0;
        error = null;

        if (def == null)
        {
            error = "Building definition is null.";
            return false;
        }

        if (GridSystem.Instance == null)
        {
            error = "GridSystem not found.";
            return false;
        }

        if (!GridSystem.Instance.WorldToGrid(worldPosition, out gridX, out gridY))
        {
            error = "Position is outside grid bounds.";
            return false;
        }

        int areaWidth = def.size != null ? def.size.width : 1;
        int areaDepth = def.size != null ? def.size.depth : 1;

        if (areaWidth <= 0 || areaDepth <= 0)
        {
            error = "Invalid building size.";
            return false;
        }

        if (OccupancyMap.Instance != null && !OccupancyMap.Instance.IsAreaFree(gridX, gridY, areaWidth, areaDepth))
        {
            error = $"Area ({gridX},{gridY}) {areaWidth}x{areaDepth} is not free.";
            return false;
        }

        if (!ValidateZone(def, gridX, gridY, out error))
            return false;

        return true;
    }

    private bool ValidateZone(BuildingDefinition def, int gridX, int gridY, out string error)
    {
        error = null;

        // مباني الخدمات لا تتطلب RCI Zone
        if (def.type == "service")
            return true;

        if (def.zoneTags == null || def.zoneTags.Count == 0)
            return true;

        if (ZoneSystem.Instance == null)
        {
            error = "ZoneSystem not found.";
            return false;
        }

        ZoneData zone = ZoneSystem.Instance.GetZone(gridX, gridY);
        if (zone == null)
        {
            error = "This area is not zoned.";
            return false;
        }

        string requiredTag = null;
        if (def.zoneTags.Contains("residential"))
            requiredTag = "residential";
        else if (def.zoneTags.Contains("commercial"))
            requiredTag = "commercial";
        else if (def.zoneTags.Contains("industrial"))
            requiredTag = "industrial";

        if (string.IsNullOrEmpty(requiredTag))
        {
            error = "Building has no valid RCI zone tag.";
            return false;
        }

        string zoneTag = zone.zoneType switch
        {
            ZoneType.Residential => "residential",
            ZoneType.Commercial => "commercial",
            ZoneType.Industrial => "industrial",
            _ => null
        };

        if (zoneTag != requiredTag)
        {
            error = $"Building requires {requiredTag} zone but this is {zoneTag}.";
            return false;
        }

        return true;
    }
}
