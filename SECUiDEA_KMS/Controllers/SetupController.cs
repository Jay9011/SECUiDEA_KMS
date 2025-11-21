using CryptoManager;
using Microsoft.AspNetCore.Mvc;
using SECUiDEA_KMS.Middleware;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.Settings;
using SECUiDEA_KMS.Models.Setup;
using SECUiDEA_KMS.Services;
using SECUiDEACryptoManager.Services;
using System.Runtime.Versioning;

namespace SECUiDEA_KMS.Controllers;

/// <summary>
/// 초기 설치 및 마스터 키 관리 컨트롤러 (2단계 설정)
/// Step 1: 마스터 키 설정
/// Step 2: 데이터베이스 설정
/// </summary>
[SupportedOSPlatform("windows")]
[LocalhostOnly]
public class SetupController : Controller
{
    private readonly MasterKeyService _masterKeyService;
    private readonly ICryptoManager _cryptoManager;
    private readonly DatabaseSetupService _databaseSetupService;
    private readonly AppSettingsService _appSettingsService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        MasterKeyService masterKeyService,
        ICryptoManager cryptoManager,
        DatabaseSetupService databaseSetupService,
        AppSettingsService appSettingsService,
        ILogger<SetupController> logger)
    {
        _masterKeyService = masterKeyService;
        _cryptoManager = cryptoManager;
        _databaseSetupService = databaseSetupService;
        _appSettingsService = appSettingsService;
        _logger = logger;
    }

    #region Index - Setup 진입점 (Middleware에서 리다이렉트 처리)

    /// <summary>
    /// Setup 메인 페이지 - Middleware에서 상태에 따라 리다이렉트됨
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Completed));
    }

    #endregion

    #region Completed - 설정 완료 페이지

    /// <summary>
    /// Setup 완료 페이지 - 설정 요약 및 백업 키 다운로드
    /// </summary>
    [HttpGet]
    public IActionResult Completed()
    {
        // 마스터 키 또는 DB 설정이 없으면 Setup으로 리다이렉트
        if (!_masterKeyService.IsInitialized)
        {
            return RedirectToAction(nameof(Step1));
        }

        if (!_databaseSetupService.IsDatabaseConfigured())
        {
            return RedirectToAction(nameof(Step2));
        }

        // 설정 정보 수집
        var model = new SetupCompletedViewModel
        {
            MasterKeyFilePath = Path.Combine("Config", Consts.MasterKeyFileName),
            BackupKeyFilePath = _masterKeyService.GetBackupKeyFilePath(),
            BackupKeyExists = System.IO.File.Exists(_masterKeyService.GetBackupKeyFilePath()),
            ConfigFilePath = _appSettingsService.FilePath
        };

        // DB 설정 정보 가져오기
        try
        {
            var dbSettings = _appSettingsService.ReadValue<MsSqlDbSettings>(Consts.Key_DB_SECUiDEA);
            if (dbSettings != null)
            {
                model.DatabaseServer = dbSettings.Server ?? "N/A";
                model.DatabaseName = dbSettings.Database ?? "N/A";
                model.DatabasePort = dbSettings.Port ?? 1433;
                model.IntegratedSecurity = dbSettings.IntegratedSecurity;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB 설정 정보를 읽는 중 오류 발생");
        }

        return View(model);
    }

    #endregion

    #region Step 1 - 마스터 키 설정

    /// <summary>
    /// Step 1: 마스터 키 설정 페이지
    /// </summary>
    [HttpGet]
    public IActionResult Step1()
    {
        var status = _masterKeyService.GetInitializationStatus();

        // 이미 초기화되어 있으면 Step2로
        if (_masterKeyService.IsInitialized)
        {
            return RedirectToAction(nameof(Step2));
        }

        var model = new SetupViewModel
        {
            Status = status,
            IsInitialized = _masterKeyService.IsInitialized,
            NeedBackupRecovery = status == EInitializationStatus.NeedBackupRecovery,
            NeedNewKey = status == EInitializationStatus.NeedNewKey,
            IsKeyFileCorrupted = status == EInitializationStatus.KeyFileCorrupted,
            StatusMessage = GetStatusMessage(status),
            CurrentStep = 1,
            TotalSteps = 2
        };

        return View(model);
    }

    /// <summary>
    /// 새 마스터 키 생성
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMasterKey([FromBody] CreateKeyReqDTO request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            var result = await _masterKeyService.CreateNewMasterKeyAsync(request.BackupPassword);

            if (result.Success)
            {
                // ICryptoManager에 키 설정
                SetCryptoManagerKey();

                _logger.LogInformation("마스터 키가 성공적으로 생성되었습니다.");

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    backupFilePath = _masterKeyService.GetBackupKeyFilePath(),
                    nextStep = Url.Action(nameof(Step2))
                });
            }

            return Ok(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "마스터 키 생성 중 예외 발생");
            return StatusCode(500, new { success = false, message = "마스터 키 생성 중 오류가 발생했습니다." });
        }
    }

    /// <summary>
    /// 백업에서 복구
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecoverFromBackup([FromBody] RecoverKeyReqDTO request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            var result = await _masterKeyService.RecoverFromBackupAsync(request.RecoveryPassword);

            if (result.Success)
            {
                // ICryptoManager에 키 설정
                SetCryptoManagerKey();

                _logger.LogInformation("마스터 키가 성공적으로 복구되었습니다.");

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    nextStep = Url.Action(nameof(Step2))
                });
            }

            return Ok(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "마스터 키 복구 중 예외 발생");
            return StatusCode(500, new { success = false, message = "마스터 키 복구 중 오류가 발생했습니다." });
        }
    }

    #endregion

    #region Step 2 - 데이터베이스 설정

    /// <summary>
    /// Step 2: 데이터베이스 설정 페이지
    /// </summary>
    [HttpGet]
    public IActionResult Step2()
    {
        // 마스터 키가 없으면 Step1으로
        if (!_masterKeyService.IsInitialized)
        {
            return RedirectToAction(nameof(Step1));
        }

        // 이미 DB가 설정되어 있으면 완료 페이지로
        if (_databaseSetupService.IsDatabaseConfigured())
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new SetupViewModel
        {
            IsInitialized = true,
            CurrentStep = 2,
            TotalSteps = 2,
            StatusMessage = "데이터베이스 연결 정보를 입력하세요."
        };

        return View(model);
    }

    /// <summary>
    /// 데이터베이스 연결 테스트
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestDatabaseConnection([FromBody] MsSqlDbSettings settings)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            var result = await _databaseSetupService.TestDatabaseConnectionPublicAsync(settings);

            return Ok(new
            {
                success = result.IsSuccess,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터베이스 연결 테스트 중 예외 발생");
            return StatusCode(500, new { success = false, message = "연결 테스트 중 오류가 발생했습니다." });
        }
    }

    /// <summary>
    /// 데이터베이스 설정 저장
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDatabaseSettings([FromBody] MsSqlDbSettings settings)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        try
        {
            // 1. 연결 테스트
            var testResult = await _databaseSetupService.TestDatabaseConnectionPublicAsync(settings);
            if (!testResult.IsSuccess)
            {
                return Ok(new { success = false, message = $"연결 테스트 실패: {testResult.Message}" });
            }

            // 2. 민감한 정보 암호화
            if (_cryptoManager is SafetyAES256 safetyAES && safetyAES.IsKeySetted)
            {
                settings.UserId = safetyAES.Encrypt(settings.UserId);
                settings.Password = safetyAES.Encrypt(settings.Password);
            }
            else
            {
                return StatusCode(500, new { success = false, message = "암호화 서비스가 준비되지 않았습니다." });
            }

            // 3. 설정 저장
            _databaseSetupService.SaveDatabaseSettings(settings);

            _logger.LogInformation("데이터베이스 설정이 저장되었습니다.");

            return Ok(new
            {
                success = true,
                message = "데이터베이스 설정이 저장되었습니다.",
                redirectUrl = Url.Action("Index", "Home")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터베이스 설정 저장 중 예외 발생");
            return StatusCode(500, new { success = false, message = "설정 저장 중 오류가 발생했습니다." });
        }
    }

    #endregion

    #region Utility Actions

    /// <summary>
    /// 초기화 상태 확인 (AJAX용)
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus()
    {
        var status = _masterKeyService.GetInitializationStatus();

        return Ok(new
        {
            isInitialized = _masterKeyService.IsInitialized,
            status = status.ToString(),
            statusMessage = GetStatusMessage(status),
            isDatabaseConfigured = _databaseSetupService.IsDatabaseConfigured(),
            cryptoManagerReady = _cryptoManager is SafetyAES256 safetyAES && safetyAES.IsKeySetted,
            currentStep = !_masterKeyService.IsInitialized ? 1 : (!_databaseSetupService.IsDatabaseConfigured() ? 2 : 0)
        });
    }

    /// <summary>
    /// 백업 키 파일 다운로드
    /// </summary>
    [HttpGet]
    public IActionResult DownloadBackupKey()
    {
        try
        {
            var backupFilePath = _masterKeyService.GetBackupKeyFilePath();

            if (!System.IO.File.Exists(backupFilePath))
            {
                return NotFound(new { success = false, message = "백업 키 파일을 찾을 수 없습니다." });
            }

            var fileBytes = System.IO.File.ReadAllBytes(backupFilePath);
            var fileName = Path.GetFileName(backupFilePath);

            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "백업 키 다운로드 중 오류 발생");
            return StatusCode(500, new { success = false, message = "백업 키 다운로드 중 오류가 발생했습니다." });
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// ICryptoManager에 마스터 키 설정
    /// </summary>
    private void SetCryptoManagerKey()
    {
        if (_masterKeyService.IsInitialized && _cryptoManager is SafetyAES256 safetyAES)
        {
            if (!safetyAES.IsKeySetted)
            {
                var keyString = Convert.ToHexString(_masterKeyService.MasterKey);
                safetyAES.SetKey(keyString);
                _logger.LogInformation("ICryptoManager에 마스터 키를 설정했습니다.");
            }
        }
    }

    /// <summary>
    /// 상태 메시지 반환
    /// </summary>
    private string GetStatusMessage(EInitializationStatus status)
    {
        return status switch
        {
            EInitializationStatus.Initialized => "마스터 키가 정상적으로 로드되었습니다.",
            EInitializationStatus.NeedNewKey => "새 마스터 키를 생성해야 합니다.",
            EInitializationStatus.NeedBackupRecovery => "백업 키로부터 복구가 필요합니다.",
            EInitializationStatus.KeyFileCorrupted => "키 파일이 손상되었습니다. 백업에서 복구하세요.",
            _ => "알 수 없는 상태입니다."
        };
    }

    #endregion
}
