using System.ComponentModel.DataAnnotations;

namespace SECUiDEA_KMS.Models.Setup;

/// <summary>
/// 마스터 키 생성 요청
/// </summary>
public class CreateKeyReqDTO
{
    [Required(ErrorMessage = "백업 암호를 입력하세요.")]
    [MinLength(8, ErrorMessage = "백업 암호는 최소 8자 이상이어야 합니다.")]
    public string BackupPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "백업 암호 확인을 입력하세요.")]
    [Compare(nameof(BackupPassword), ErrorMessage = "백업 암호가 일치하지 않습니다.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// 백업 복구 요청
/// </summary>
public class RecoverKeyReqDTO
{
    [Required(ErrorMessage = "복구 암호를 입력하세요.")]
    public string RecoveryPassword { get; set; } = string.Empty;
}

