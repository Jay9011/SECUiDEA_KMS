using System.Text.Json;

namespace SECUiDEA_KMS.Utils;

public class DictionaryExtensions
{
    public static Dictionary<string, object?> ToDictionary<T>(T value) where T : class
    {
        var JsonElement = JsonSerializer.SerializeToElement(value);
        var result = new Dictionary<string, object?>();
        foreach (var property in JsonElement.EnumerateObject())
        {
            result[property.Name] = property.Value.ConvertToObject();
        }
        return result;
    }
}
