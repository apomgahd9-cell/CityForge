using UnityEngine;

public class BuildingInstance
{
    public string definitionId;
    public BuildingDefinition Definition { get; private set; }
    
    // المستوى الحالي للمبنى
    public int CurrentLevel { get; private set; }

    public BuildingInstance(BuildingDefinition definition)
    {
        Definition = definition;
        definitionId = definition.id;
        // يبدأ المبنى دائماً بالمستوى 1
        CurrentLevel = 1;
    }

    // تعيين مستوى جديد للمبنى
    public void SetLevel(int newLevel)
    {
        if (newLevel < 1)
        {
            Debug.LogWarning($"Attempted to set invalid level {newLevel} for building {Definition.id}");
            return;
        }
        CurrentLevel = newLevel;
    }
}
