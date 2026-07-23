public class BuildingInstance
{
    public BuildingDefinition Definition { get; private set; }


    public BuildingInstance(BuildingDefinition definition)
    {
        Definition = definition;
    }
}
