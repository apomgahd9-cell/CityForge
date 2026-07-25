public interface ISaveable
{
    int LoadPriority { get; }
    void Save(SaveData data);
    void Load(SaveData data);
}
