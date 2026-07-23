using UnityEngine;


public class BuildingSpawner : MonoBehaviour
{
    public static BuildingSpawner Instance { get; private set; }



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }



    public BuildingInstance SpawnBuilding(string buildingId)
    {
        if (DataRegistry.Instance == null)
        {
            Debug.LogError("DataRegistry missing.");
            return null;
        }


        BuildingDefinition definition =
            DataRegistry.Instance.GetBuilding(buildingId);


        if (definition == null)
        {
            Debug.LogError(
                $"Building not found: {buildingId}"
            );

            return null;
        }


        BuildingInstance instance =
            new BuildingInstance(definition);



        if (MetricsSystem.Instance != null)
        {
            MetricsSystem.Instance.AddBuilding(instance);
        }


        Debug.Log(
            $"Building spawned: {definition.displayName}"
        );


        return instance;
    }
}
