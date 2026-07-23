using UnityEngine;


public class RCISystem : MonoBehaviour
{
    public static RCISystem Instance { get; private set; }


    public float residential_demand { get; private set; }
    public float commercial_demand { get; private set; }
    public float industrial_demand { get; private set; }



    private void Awake()
    {
        Instance = this;
    }



    private void Start()
    {
        if (BuildingSpawner.Instance != null)
        {
            BuildingSpawner.Instance.SpawnBuilding(
                "residential_house_01"
            );

            BuildingSpawner.Instance.SpawnBuilding(
                "commercial_shop_01"
            );

            BuildingSpawner.Instance.SpawnBuilding(
                "industrial_factory_01"
            );
        }
    }



    private void Update()
    {
        Calculate();
    }



    private void Calculate()
    {
        if (MetricsSystem.Instance == null)
            return;


        float population =
            MetricsSystem.Instance.GetMetric(
                "population_total"
            );


        float jobs =
            MetricsSystem.Instance.GetMetric(
                "jobs_available"
            );


        float freight =
            MetricsSystem.Instance.GetMetric(
                "freight_access"
            );



        residential_demand =
            Mathf.Clamp(
                (jobs > 0 && population > 0 ?
                jobs / population * 0.8f : 0),
                -100,
                100
            );



        commercial_demand =
            Mathf.Clamp(
                5 +
                population * 0.005f +
                residential_demand * 0.3f,
                -100,
                100
            );



        industrial_demand =
            Mathf.Clamp(
                5 +
                freight * 0.1f,
                -100,
                100
            );


        Debug.Log(
            $"RCI => R:{residential_demand:F2} C:{commercial_demand:F2} I:{industrial_demand:F2}"
        );
    }
}
