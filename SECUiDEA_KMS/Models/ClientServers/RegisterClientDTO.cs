using System.ComponentModel.DataAnnotations;

namespace SECUiDEA_KMS.Models.ClientServers;

/// <summary>
/// 클라이언트 등록 요청
/// </summary>
public class RegisterClientDTO
{
    /// <summary>
    /// 클라이언트 이름
    /// </summary>
    [Required(ErrorMessage = "클라이언트 이름은 필수입니다.")]
    [StringLength(200, ErrorMessage = "클라이언트 이름은 200자 이내여야 합니다.")]
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 클라이언트 IP 주소
    /// </summary>
    [Required(ErrorMessage = "IP 주소는 필수입니다.")]
    [StringLength(50, ErrorMessage = "IP 주소는 50자 이내여야 합니다.")]
    [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$|^(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", 
        ErrorMessage = "유효한 IP 주소를 입력하세요.")]
    public string ClientIP { get; set; } = string.Empty;

    /// <summary>
    /// IP 검증 모드 (Strict, None)
    /// </summary>
    [Required(ErrorMessage = "IP 검증 모드는 필수입니다.")]
    [RegularExpression("^(Strict|None)$", ErrorMessage = "IP 검증 모드는 'Strict' 또는 'None'이어야 합니다.")]
    public string IPValidationMode { get; set; } = "Strict";

    /// <summary>
    /// 설명
    /// </summary>
    [StringLength(500, ErrorMessage = "설명은 500자 이내여야 합니다.")]
    public string? Description { get; set; }
}

