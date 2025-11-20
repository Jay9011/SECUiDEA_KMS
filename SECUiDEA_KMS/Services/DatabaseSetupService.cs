using CoreDAL.Configuration;
using SECUiDEA_KMS.Models;

namespace SECUiDEA_KMS.Services;

/// <summary>
/// 데이터베이스 설정 서비스
/// - 데이터베이스 연결 테스트
/// - 설정 저장
/// </summary>
public class DatabaseSetupService
{
    private readonly AppSettingsService _appSettingsService;
    private readonly ILogger<DatabaseSetupService> _logger;

    public DatabaseSetupService(
        AppSettingsService appSettingsService,
        ILogger<DatabaseSetupService> logger)
    {
        _appSettingsService = appSettingsService;
        _logger = logger;
    }

    /// <summary>
    /// 데이터베이스 설정 완료 여부 확인
    /// </summary>
    public bool IsDatabaseConfigured()
    {
        return _appSettingsService.SectionExists(Consts.Key_DB_SECUiDEA);
    }

    /// <summary>
    /// 데이터베이스 연결 테스트
    /// </summary>
    private async Task<(bool IsSuccess, string Message)> TestDatabaseConnectionAsync(MsSqlDbSettings settings)
    {
        try
        {
            // Validate 메서드 호출
            if (!settings.Validate(out string errorMessage))
            {
                return (false, errorMessage);
            }

            // 연결 문자열 생성
            var connectionString = settings.ToConnectionString();

            // CoreDAL을 사용한 연결 테스트
            // 주의: DbDalFactory는 CoreDAL 패키지에서 제공됨
            var testResult = await DbDalFactory.CreateCoreDal(DatabaseType.MSSQL)
                .TestConnectionAsync(connectionString);

            return testResult.IsSuccess
                ? (true, testResult.Message ?? "연결 성공")
                : (false, testResult.Message ?? "연결 실패");
        }
        catch (Exception ex)
        {
            return (false, $"연결 테스트 중 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 데이터베이스 설정 저장 (암호화된 상태로)
    /// </summary>
    public void SaveDatabaseSettings(MsSqlDbSettings settings)
    {
        _appSettingsService.WriteValue(Consts.Key_DB_SECUiDEA, settings);
        _logger.LogInformation("데이터베이스 설정이 저장되었습니다.");
    }

    /// <summary>
    /// 데이터베이스 연결 테스트 (public 버전)
    /// </summary>
    public async Task<(bool IsSuccess, string Message)> TestDatabaseConnectionPublicAsync(MsSqlDbSettings settings)
    {
        return await TestDatabaseConnectionAsync(settings);
    }
}
