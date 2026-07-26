using Newtonsoft.Json;
using UnityEngine;

public static class JsonLoader
{
    public static T Load<T>(string pathWithoutExtension) where T : class
    {
        TextAsset textAsset = Resources.Load<TextAsset>(pathWithoutExtension);
        if (textAsset == null)
        {
            Debug.LogError($"File not found in Resources: {pathWithoutExtension}");
            return null;
        }

        return JsonConvert.DeserializeObject<T>(textAsset.text);
    }

    public static T Load<T>(string pathWithoutExtension, JsonSerializerSettings settings) where T : class
    {
        TextAsset textAsset = Resources.Load<TextAsset>(pathWithoutExtension);
        if (textAsset == null)
        {
            Debug.LogError($"File not found in Resources: {pathWithoutExtension}");
            return null;
        }

        return JsonConvert.DeserializeObject<T>(textAsset.text, settings);
    }
}
