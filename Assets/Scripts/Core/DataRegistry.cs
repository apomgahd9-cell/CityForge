using System.Collections.Generic;
using UnityEngine;

public class DataRegistry : MonoBehaviour
{
    public static DataRegistry Instance { get; private set; }

    public Dictionary<string, BuildingDefinition> Buildings { get; private set; }
        = new Dictionary<string, BuildingDefinition>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }


    private void Initialize()
    {
        LoadBuildings();

        Debug.Log("DataRegistry initialized.");
    }


    private void LoadBuildings()
    {
        BuildingListWrapper wrapper =
            JsonLoader.Load<BuildingListWrapper>(
                "Data/Definitions/buildings"
            );


        if (wrapper == null || wrapper.buildings == null)
            return;


        foreach (BuildingDefinition building in wrapper.buildings)
        {
            if (!string.IsNullOrEmpty(building.id))
            {
                Buildings[building.id] = building;

                Debug.Log(
                    $"Building loaded: {building.id} - {building.displayName}"
                );
            }
        }
    }


    public BuildingDefinition GetBuilding(string id)
    {
        Buildings.TryGetValue(id, out BuildingDefinition building);
        return building;
    }
}



[System.Serializable]
public class BuildingListWrapper
{
    public List<BuildingDefinition> buildings;
}
