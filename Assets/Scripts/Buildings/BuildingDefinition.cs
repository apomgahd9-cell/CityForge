using System;
using System.Collections.Generic;


[Serializable]
public class BuildingDefinition
{
    public string id;
    public string type;
    public string displayName;
    public string category;

    public List<string> zoneTags;

    public string growthProfile;

    public int level;

    public string model;


    public Size size;

    public Construction construction;

    public Outputs outputs;

    public Stats stats;


    public string upgradesTo;

    public List<string> variants;



    [Serializable]
    public class Size
    {
        public int width;
        public int depth;
    }


    [Serializable]
    public class Construction
    {
        public int cost;
        public int timeTicks;
    }


    [Serializable]
    public class Outputs
    {
        public RangeInt population;

        public RangeInt jobs_available;

        public int customer_access;

        public int freight_access;

        public int pollution;
    }


    [Serializable]
    public class Stats
    {
        public int power_usage;
        public int water_usage;
        public int garbage_production;
        public int happiness_base;
    }


    [Serializable]
    public class RangeInt
    {
        public int min;
        public int max;
    }
}
