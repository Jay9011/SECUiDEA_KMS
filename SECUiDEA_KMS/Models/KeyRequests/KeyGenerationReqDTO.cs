using System.ComponentModel.DataAnnotations;

namespace SECUiDEA_KMS.Models.KeyRequests;

/// <summary>
/// 키 생성 요청
/// </summary>
public class KeyGenerationReqDTO
{
    /// <summary>
    /// 클라이언트 GUID
    /// </summary>
    [Required(ErrorMessage = "클라이언트 GUID는 필수입니다.")]
    public Guid ClientGuid { get; set; }

    /// <summary>
    /// 자동 회전 여부 (0: 반영구적, 1: 자동 회전)
    /// </summary>
    public bool IsAutoRotation { get; set; } = false;

    /// <summary>
    /// 만료 일수 (IsAutoRotation이 true일 때 필수)
    /// </summary>
    [Range(1, 3650, ErrorMessage = "만료 일수는 1일 ~ 3650일 사이여야 합니다.")]
    public int? ExpirationDays { get; set; }

    /// <summary>
    /// 회전 스케줄 일수 (IsAutoRotation이 true일 때 필수)
    /// </summary>
    [Range(1, 365, ErrorMessage = "회전 스케줄은 1일 ~ 365일 사이여야 합니다.")]
    public int? RotationScheduleDays { get; set; }
}

