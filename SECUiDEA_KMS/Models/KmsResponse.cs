namespace SECUiDEA_KMS.Models;

/// <summary>
/// KMS API 공통 응답 래퍼
/// 모든 프로시저는 ErrorCode와 ErrorMessage를 반환
/// </summary>
/// <typeparam name="T">반환 데이터 타입</typeparam>
public class KmsResponse<T>
{
    /// <summary>
    /// 에러 코드 ('0000': 성공, 기타: 에러)
    /// </summary>
    public string ErrorCode { get; set; } = "0000";

    /// <summary>
    /// 에러 메시지 ('Success' 또는 에러 메시지)
    /// </summary>
    public string ErrorMessage { get; set; } = "Success";

    /// <summary>
    /// 응답 데이터
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool IsSuccess => ErrorCode == "0000";
}

/// <summary>
/// 데이터 없는 응답용
/// </summary>
public class KmsResponse : KmsResponse<object>
{
}

