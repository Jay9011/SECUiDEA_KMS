namespace SECUiDEA_KMS.Models;

/// <summary>
/// HTTP 요청 정보
/// 프로시저 호출 시 전달되는 요청 메타데이터
/// </summary>
public class RequestInfo
{
    /// <summary>
    /// 요청 IP 주소
    /// </summary>
    public string? RequestIP { get; set; }

    /// <summary>
    /// 요청 UserAgent
    /// </summary>
    public string? RequestUserAgent { get; set; }

    /// <summary>
    /// 요청 Host
    /// </summary>
    public string? RequestHost { get; set; }

    /// <summary>
    /// 요청 Path
    /// </summary>
    public string? RequestPath { get; set; }
}

