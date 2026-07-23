using Newtonsoft.Json;
using UnityEngine;

public static class JsonLoader
{
    public static T Load<T>(string path) where T : class
    {
        TextAsset file = Resources.Load<TextAsset>(path);

        if (file == null)
        {
            Debug.LogError($"JSON file not found: {path}");
            return null;
        }

        return JsonConvert.DeserializeObject<T>(file.text);
    }
}
