using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour, ISaveable
{
    public static BuildingSpawner Instance { get; private set; }

    private List<BuildingInstance> activeBuildings = new List<BuildingInstance>();

    public int LoadPriority => 20;

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

    private void Start()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.Unregister(this);
    }

    public BuildingInstance SpawnBuilding(string buildingId)
    {
        return SpawnBuilding(buildingId, Vector3.zero);
    }

    public BuildingInstance SpawnBuilding(string buildingId, Vector3 position)
    {
        if (DataRegistry.Instance == null)
        {
            Debug.LogError("DataRegistry not found.");
            return null;
        }

        BuildingDefinition definition = DataRegistry.Instance.GetBuilding(buildingId);
        if (definition == null)
        {
            Debug.LogError($"Building definition not found: {buildingId}");
            return null;
        }

        BuildingInstance instance = new BuildingInstance(definition, position);
        activeBuildings.Add(instance);

        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.AddBuilding(instance);
        else
            Debug.LogWarning("MetricsSystem not available. Building not added to metrics.");

        Debug.Log($"Building spawned: {definition.displayName} (ID: {definition.id}) at {position}");
        return instance;
    }

    public IReadOnlyList<BuildingInstance> GetAllBuildings()
    {
        return activeBuildings.AsReadOnly();
    }

    public void RemoveBuilding(BuildingInstance building, bool freeOccupancy = false)
    {
        if (!activeBuildings.Contains(building)) return;

        if (freeOccupancy && OccupancyMap.Instance != null && building.OccupantId > 0)
        {
            OccupancyMap.Instance.FreeAreaByOccupantId(building.OccupantId);
        }

        activeBuildings.Remove(building);
        if (MetricsSystem.Instance != null)
            MetricsSystem.Instance.RemoveBuilding(building);

        Debug.Log($"Building removed: {building.Definition.displayName}");
    }

    public bool ReplaceBuilding(BuildingInstance building, string newDefinitionId)
    {
        if (building == null)
        {
            Debug.LogWarning("ReplaceBuilding: building is null.");
            return false;
        }

        if (DataRegistry.Instance == null)
        {
            Debug.LogWarning("DataRegistry not available.");
            return false;
        }

        BuildingDefinition newDef = DataRegistry.Instance.GetBuilding(newDefinitionId);
        if (newDef == null)
        {
            Debug.LogWarning($"ReplaceBuilding: definition not found: {newDefinitionId}");
            return false;
        }

        if (PlacementValidator.Instance == null)
        {
            Debug.LogWarning("ReplaceBuilding: PlacementValidator not available.");
            return false;
        }

        // طبقة التحقق الموحدة قبل أي تغيير
        if (!PlacementValidator.Instance.ValidateReplacement(
            building,
            newDef,
            out int gridX,
            out int gridY,
            out string validationError))
        {
            Debug.LogWarning($"ReplaceBuilding rejected: {validationError}");
            return false;
        }

        if (GridSystem.Instance == null || OccupancyMap.Instance == null)
        {
            Debug.LogWarning("ReplaceBuilding: GridSystem or OccupancyMap not available.");
            return false;
        }

        int oldWidth = building.Definition.size != null ? building.Definition.size.width : 1;
        int oldDepth = building.Definition.size != null ? building.Definition.size.depth : 1;
        int newWidth = newDef.size != null ? newDef.size.width : 1;
        int newDepth = newDef.size != null ? newDef.size.depth : 1;

        string oldName = building.Definition.displayName;
        int oldOccupantId = building.OccupantId;
        TileType oldTileType = GetTileTypeForDefinition(building.Definition);

        // 1) تحرير الإشغال القديم
        if (oldOccupantId > 0)
        {
            OccupancyMap.Instance.FreeAreaByOccupantId(oldOccupantId);
        }

        // 2) مسح البلاطات القديمة
        ClearTiles(gridX, gridY, oldWidth, oldDepth);

        // 3) حجز المساحة الجديدة
        int newOccupantId = OccupancyMap.Instance.OccupyArea(gridX, gridY, newWidth, newDepth);

        if (newOccupantId < 0)
        {
            Debug.LogError("ReplaceBuilding: OccupyArea failed. Rolling back.");
            RollbackState(building, gridX, gridY, oldWidth, oldDepth, oldOccupantId, oldTileType);
            return false;
        }

        // 4) تغيير التعريف بعد نجاح الحجز
        building.ReplaceDefinition(newDef);
        building.SetOccupantId(newOccupantId);

        // 5) تحديث البلاطات الجديدة
        SetTiles(gridX, gridY, newWidth, newDepth, GetTileTypeForDefinition(newDef));

        // 6) إعادة حساب المقاييس
        MetricsSystem.Instance?.RecalculateAll();

        Debug.Log($"Building replaced: {oldName} → {newDef.displayName} ({oldWidth}x{oldDepth} → {newWidth}x{newDepth})");
        return true;
    }

    private void RollbackState(
        BuildingInstance building,
        int gridX,
        int gridY,
        int oldWidth,
        int oldDepth,
        int oldOccupantId,
        TileType oldTileType)
    {
        if (OccupancyMap.Instance == null || GridSystem.Instance == null)
        {
            Debug.LogError("RollbackState: cannot rollback without OccupancyMap and GridSystem.");
            building.SetOccupantId(0);
            return;
        }

        // إزالة أي إشغال جزئي محتمل
        if (building.OccupantId > 0 && building.OccupantId != oldOccupantId)
        {
            OccupancyMap.Instance.FreeAreaByOccupantId(building.OccupantId);
        }

        // محاولة استعادة المساحة القديمة
        int rollbackId = OccupancyMap.Instance.OccupyArea(gridX, gridY, oldWidth, oldDepth);

        if (rollbackId < 0)
        {
            Debug.LogError("RollbackState: failed to reoccupy old area. Replacement failed and rollback incomplete. System may be inconsistent.");
            building.SetOccupantId(0);
            return;
        }

        building.SetOccupantId(rollbackId);
        SetTiles(gridX, gridY, oldWidth, oldDepth, oldTileType);
    }

    private void ClearTiles(int startX, int startY, int areaWidth, int areaDepth)
    {
        if (GridSystem.Instance == null) return;

        for (int x = startX; x < startX + areaWidth; x++)
        {
            for (int y = startY; y < startY + areaDepth; y++)
            {
                GridSystem.Instance.SetTile(x, y, new TileData
                {
                    gridX = x,
                    gridY = y,
                    type = TileType.Empty
                });
            }
        }
    }

    private void SetTiles(int startX, int startY, int areaWidth, int areaDepth, TileType tileType)
    {
        if (GridSystem.Instance == null) return;

        for (int x = startX; x < startX + areaWidth; x++)
        {
            for (int y = startY; y < startY + areaDepth; y++)
            {
                GridSystem.Instance.SetTile(x, y, new TileData
                {
                    gridX = x,
                    gridY = y,
                    type = tileType
                });
            }
        }
    }

    public void Save(SaveData data)
    {
        data.buildings.Clear();
        foreach (BuildingInstance building in activeBuildings)
        {
            data.buildings.Add(new BuildingSaveData
            {
                definitionId = building.Definition.id,
                currentLevel = building.CurrentLevel,
                positionX = building.Position.x,
                positionY = building.Position.y,
                positionZ = building.Position.z
            });
        }
    }

    public void Load(SaveData data)
    {
        while (activeBuildings.Count > 0)
            RemoveBuilding(activeBuildings[0], freeOccupancy: true);

        foreach (BuildingSaveData saved in data.buildings)
        {
            Vector3 position = new Vector3(saved.positionX, saved.positionY, saved.positionZ);
            BuildingInstance instance = SpawnBuilding(saved.definitionId, position);

            if (instance != null)
            {
                instance.SetLevel(saved.currentLevel);

                BuildingDefinition def = instance.Definition;
                int areaWidth = def.size != null ? def.size.width : 1;
                int areaDepth = def.size != null ? def.size.depth : 1;

                if (OccupancyMap.Instance != null && GridSystem.Instance != null)
                {
                    if (GridSystem.Instance.WorldToGrid(position, out int gridX, out int gridY))
                    {
                        int occupantId = OccupancyMap.Instance.ReoccupyArea(gridX, gridY, areaWidth, areaDepth);
                        instance.SetOccupantId(occupantId);

                        TileType tileType = GetTileTypeForDefinition(def);
                        SetTiles(gridX, gridY, areaWidth, areaDepth, tileType);
                    }
                }
            }
        }
    }

    private TileType GetTileTypeForDefinition(BuildingDefinition definition)
    {
        if (definition.zoneTags != null)
        {
            if (definition.zoneTags.Contains("residential"))
                return TileType.Residential;
            if (definition.zoneTags.Contains("commercial"))
                return TileType.Commercial;
            if (definition.zoneTags.Contains("industrial"))
                return TileType.Industrial;
        }

        return TileType.Service;
    }
}
