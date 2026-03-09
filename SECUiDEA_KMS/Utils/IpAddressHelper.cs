using System.Net;

namespace SECUiDEA_KMS.Utils;

public static class IpAddressHelper
{
    /// <summary>
    /// IP 주소를 비교 가능한 표준 문자열로 정규화
    /// </summary>
    public static string? Normalize(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return ipAddress;
        }

        // X-Forwarded-For는 여러 IP를 콤마로 전달할 수 있으므로 첫 번째 IP만 사용
        var candidate = ipAddress.Split(',')[0].Trim();

        if (!IPAddress.TryParse(candidate, out var parsedIp))
        {
            return candidate;
        }

        if (IPAddress.IsLoopback(parsedIp))
        {
            return IPAddress.Loopback.ToString();
        }

        if (parsedIp.IsIPv4MappedToIPv6)
        {
            parsedIp = parsedIp.MapToIPv4();
        }

        return parsedIp.ToString();
    }

    public static bool AreEquivalent(string? left, string? right)
    {
        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }
}
