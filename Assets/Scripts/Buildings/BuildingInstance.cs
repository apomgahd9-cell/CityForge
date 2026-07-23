using UnityEngine;

public class BuildingInstance
{
    public string definitionId;
    public BuildingDefinition Definition { get; private set; }
    public int CurrentLevel { get; private set; }

    // قيم ثابتة تُحدد عند الإنشاء ولا تتغير إلا بترقية المبنى
    public int Population { get; private set; }
    public int Jobs { get; private set; }

    public BuildingInstance(BuildingDefinition definition)
    {
        Definition = definition;
        definitionId = definition.id;
        CurrentLevel = 1;

        // توليد القيم العشوائية مرة واحدة فقط
        if (definition.outputs.population != null)
        {
            Population = Random.Range(definition.outputs.population.min, definition.outputs.population.max + 1);
        }
        else
        {
            Population = 0;
        }

        if (definition.outputs.jobs_available != null)
        {
            Jobs = Random.Range(definition.outputs.jobs_available.min, definition.outputs.jobs_available.max + 1);
        }
        else
        {
            Jobs = 0;
        }
    }

    public void SetLevel(int newLevel)
    {
        if (newLevel < 1)
        {
            Debug.LogWarning($"Attempted to set invalid level {newLevel} for building {Definition.id}");
            return;
        }
        CurrentLevel = newLevel;
        // في المستقبل، عند الترقية، يمكن إعادة حساب Population و Jobs بناءً على التعريف الجديد
    }
}
