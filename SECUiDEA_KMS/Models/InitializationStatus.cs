namespace SECUiDEA_KMS.Models;

/// <summary>
/// 마스터 키 초기화 상태
/// </summary>
public enum InitializationStatus
{
    /// <summary>초기화 완료</summary>
    Initialized,
    
    /// <summary>새 마스터 키 생성 필요</summary>
    NeedNewKey,
    
    /// <summary>백업 키로 복구 필요</summary>
    NeedBackupRecovery,
    
    /// <summary>키 파일 손상됨</summary>
    KeyFileCorrupted
}

