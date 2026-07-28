using UnityEngine;

public class BuildingInstance
{
    public string definitionId;
    public BuildingDefinition Definition { get; private set; }
    public int CurrentLevel { get; private set; }
    public Vector3 Position { get; private set; }
    public int OccupantId { get; private set; }

    public int Population { get; private set; }
    public int Jobs { get; private set; }

    public BuildingInstance(BuildingDefinition definition)
    {
        Definition = definition;
        definitionId = definition.id;
        CurrentLevel = 1;
        Position = Vector3.zero;
        OccupantId = 0;

        if (definition.outputs.population != null)
            Population = Random.Range(definition.outputs.population.min, definition.outputs.population.max + 1);
        else
            Population = 0;

        if (definition.outputs.jobs_available != null)
            Jobs = Random.Range(definition.outputs.jobs_available.min, definition.outputs.jobs_available.max + 1);
        else
            Jobs = 0;
    }

    public BuildingInstance(BuildingDefinition definition, Vector3 position) : this(definition)
    {
        Position = position;
    }

    public void SetLevel(int newLevel)
    {
        if (newLevel < 1)
        {
            Debug.LogWarning($"Attempted to set invalid level {newLevel} for building {Definition.id}");
            return;
        }
        CurrentLevel = newLevel;
    }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void SetOccupantId(int id)
    {
        if (id < 0)
        {
            Debug.LogWarning($"Invalid OccupantId: {id}");
            return;
        }

        OccupantId = id;
    }
}
