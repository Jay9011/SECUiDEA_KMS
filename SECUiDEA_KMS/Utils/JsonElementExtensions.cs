using System.Text.Json;

namespace SECUiDEA_KMS.Utils;

public static class JsonElementExtensions
{
    /// <summary>
    /// JsonElement를 적절한 객체 타입으로 변환
    /// </summary>
    /// <param name="element">JsonElement</param>
    /// <returns>변환된 객체</returns>
    public static object ConvertToObject(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element.Deserialize<Dictionary<string, object>>() ?? new();
            case JsonValueKind.Array:
                return element.Deserialize<List<object>>() ?? new();
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                return ConvertNumber(element);
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// JsonElement를 숫자 타입으로 변환
    /// </summary>
    /// <param name="element">JsonElement</param>
    /// <returns>변환된 숫자 타입</returns>
    private static object ConvertNumber(JsonElement element)
    {
        if (element.TryGetInt32(out int intValue))
        {
            return intValue;
        }
        if (element.TryGetInt64(out long longValue))
        {
            return longValue;
        }
        if (element.TryGetDecimal(out decimal decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }
}
