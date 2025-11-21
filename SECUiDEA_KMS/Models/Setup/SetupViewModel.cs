namespace SECUiDEA_KMS.Models.Setup;

/// <summary>
/// Setup 페이지 ViewModel
/// </summary>
public class SetupViewModel
{
    /// <summary>초기화 상태</summary>
    public EInitializationStatus Status { get; set; }

    /// <summary>초기화 완료 여부</summary>
    public bool IsInitialized { get; set; }

    /// <summary>백업 복구 필요 여부</summary>
    public bool NeedBackupRecovery { get; set; }

    /// <summary>새 키 생성 필요 여부</summary>
    public bool NeedNewKey { get; set; }

    /// <summary>키 파일 손상 여부</summary>
    public bool IsKeyFileCorrupted { get; set; }

    /// <summary>상태 메시지</summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>데이터베이스 설정 완료 여부</summary>
    public bool IsDatabaseConfigured { get; set; }

    /// <summary>현재 단계</summary>
    public int CurrentStep { get; set; }

    /// <summary>전체 단계</summary>
    public int TotalSteps { get; set; }
}

/// <summary>
/// Setup 완료 페이지 ViewModel
/// </summary>
public class SetupCompletedViewModel
{
    /// <summary>마스터 키 파일 경로</summary>
    public string MasterKeyFilePath { get; set; } = string.Empty;

    /// <summary>백업 키 파일 경로</summary>
    public string BackupKeyFilePath { get; set; } = string.Empty;

    /// <summary>백업 키 파일 존재 여부</summary>
    public bool BackupKeyExists { get; set; }

    /// <summary>데이터베이스 서버</summary>
    public string DatabaseServer { get; set; } = string.Empty;

    /// <summary>데이터베이스 이름</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>데이터베이스 포트</summary>
    public int DatabasePort { get; set; } = 1433;

    /// <summary>통합 인증 사용 여부</summary>
    public bool IntegratedSecurity { get; set; }

    /// <summary>설정 파일 경로</summary>
    public string ConfigFilePath { get; set; } = string.Empty;
}

