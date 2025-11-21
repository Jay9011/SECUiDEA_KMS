using System.ComponentModel.DataAnnotations;

namespace SECUiDEA_KMS.Models.KeyRequests;

/// <summary>
/// 키 조회 요청
/// </summary>
public class GetKeyReqDTO
{
    /// <summary>
    /// 클라이언트 GUID
    /// </summary>
    [Required(ErrorMessage = "클라이언트 GUID는 필수입니다.")]
    public Guid ClientGuid { get; set; }
}

