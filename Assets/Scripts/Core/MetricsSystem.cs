using System.Collections.Generic;
using UnityEngine;

public class MetricsSystem : MonoBehaviour
{
    public static MetricsSystem Instance { get; private set; }


    public int population_total { get; private set; }
    public int jobs_available { get; private set; }
    public int jobs_industrial { get; private set; }

    public float freight_access { get; private set; }
    public float pollution { get; private set; }


    private List<BuildingInstance> buildings =
        new List<BuildingInstance>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }



    public void AddBuilding(BuildingInstance building)
    {
        buildings.Add(building);

        Recalculate();

        Debug.Log(
            $"Metrics updated: Population={population_total}, Jobs={jobs_available}"
        );
    }



    private void Recalculate()
    {
        population_total = 0;
        jobs_available = 0;
        jobs_industrial = 0;
        freight_access = 0;
        pollution = 0;


        foreach (BuildingInstance building in buildings)
        {
            var output = building.Definition.outputs;


            if (output.population != null)
            {
                population_total += Random.Range(
                    output.population.min,
                    output.population.max + 1
                );
            }


            if (output.jobs_available != null)
            {
                int jobs = Random.Range(
                    output.jobs_available.min,
                    output.jobs_available.max + 1
                );

                jobs_available += jobs;


                if (building.Definition.type == "industrial")
                {
                    jobs_industrial += jobs;
                }
            }


            freight_access += output.freight_access;
            pollution += output.pollution;
        }
    }



    public float GetMetric(string id)
    {
        switch(id)
        {
            case "population_total":
                return population_total;

            case "jobs_available":
                return jobs_available;

            case "jobs_industrial":
                return jobs_industrial;

            case "freight_access":
                return freight_access;

            case "pollution":
                return pollution;

            default:
                return 0;
        }
    }
}
