using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using SECUiDEA_KMS.Models;

namespace SECUiDEA_KMS.Services;

/// <summary>
/// 마스터 키 관리 서비스 (WEB UI 지원)
/// Two-Phase Initialization 패턴 적용:
/// 1. 생성자: 기존 키 파일이 있으면 자동 로드
/// 2. 메서드: WEB UI에서 수동으로 초기화/복구
/// </summary>
[SupportedOSPlatform("windows")]
public class MasterKeyService
{
    #region Fields & Properties

    private byte[]? _masterKey;
    private readonly string _keyFilePath;
    private readonly string _backupKeyFilePath;
    private readonly object _lockObject = new object();
    private readonly ILogger<MasterKeyService> _logger;

    /// <summary>초기화 상태 확인</summary>
    public bool IsInitialized => _masterKey != null;

    /// <summary>
    /// 마스터 키 접근 (초기화되지 않았으면 예외 발생)
    /// </summary>
    public byte[] MasterKey
    {
        get
        {
            if (!IsInitialized)
                throw new InvalidOperationException("마스터 키가 초기화되지 않았습니다. Setup 페이지에서 초기화를 완료하세요.");
            return _masterKey!;
        }
    }

    #endregion

    #region Constructor

    public MasterKeyService(IHostEnvironment environment, ILogger<MasterKeyService> logger)
    {
        _logger = logger;
        _keyFilePath = Path.Combine(environment.ContentRootPath, Consts.Configure, Consts.MasterKeyFileName);
        _backupKeyFilePath = Path.Combine(environment.ContentRootPath, Consts.Configure, Consts.BackupMasterKeyFileName);

        // 생성자에서는 기존 키 파일만 자동 로드 (콘솔 입력 없음!)
        TryAutoLoad();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 초기화 상태 확인
    /// </summary>
    public EInitializationStatus GetInitializationStatus()
    {
        if (IsInitialized)
            return EInitializationStatus.Initialized;

        if (File.Exists(_keyFilePath))
            return EInitializationStatus.KeyFileCorrupted;

        if (File.Exists(_backupKeyFilePath))
            return EInitializationStatus.NeedBackupRecovery;

        return EInitializationStatus.NeedNewKey;
    }

    /// <summary>
    /// 새 마스터 키 생성 (WEB UI에서 호출)
    /// </summary>
    /// <param name="backupPassword">백업 키 암호화에 사용할 비밀번호</param>
    /// <returns>성공 여부와 메시지</returns>
    public Task<(bool Success, string Message)> CreateNewMasterKeyAsync(string backupPassword)
    {
        if (string.IsNullOrWhiteSpace(backupPassword))
            return Task.FromResult((false, "백업 암호를 입력하세요."));

        if (backupPassword.Length < 8)
            return Task.FromResult((false, "백업 암호는 최소 8자 이상이어야 합니다."));

        lock (_lockObject)
        {
            if (IsInitialized)
                return Task.FromResult((false, "마스터 키가 이미 초기화되어 있습니다."));

            try
            {
                _logger.LogInformation("새 마스터 키 생성을 시작합니다.");

                // 1. 새 마스터 키 생성
                _masterKey = GenerateNewMasterKey();

                // 2. DPAPI로 보호하여 저장
                SaveWithDPAPI(_masterKey);
                _logger.LogInformation("마스터 키를 DPAPI로 보호하여 저장했습니다.");

                // 3. 백업 키 생성 및 저장
                SaveBackupKey(_masterKey, backupPassword);
                _logger.LogInformation("백업 키를 생성하여 저장했습니다.");

                WriteEventLog("새 마스터 키가 성공적으로 생성되었습니다.", EventLogEntryType.Information);

                return Task.FromResult((true, $"마스터 키가 성공적으로 생성되었습니다. 백업 키 파일({Consts.BackupMasterKeyFileName})을 안전한 곳에 보관하세요."));
            }
            catch (Exception ex)
            {
                _masterKey = null;
                _logger.LogError(ex, "마스터 키 생성 중 오류 발생");
                WriteEventLog($"마스터 키 생성 실패: {ex.Message}", EventLogEntryType.Error);
                return Task.FromResult((false, $"마스터 키 생성 실패: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// 백업 키로부터 마스터 키 복구 (WEB UI에서 호출)
    /// </summary>
    /// <param name="recoveryPassword">백업 키 복호화 비밀번호</param>
    /// <returns>성공 여부와 메시지</returns>
    public Task<(bool Success, string Message)> RecoverFromBackupAsync(string recoveryPassword)
    {
        if (string.IsNullOrWhiteSpace(recoveryPassword))
            return Task.FromResult((false, "복구 암호를 입력하세요."));

        lock (_lockObject)
        {
            if (IsInitialized)
                return Task.FromResult((false, "마스터 키가 이미 초기화되어 있습니다."));

            if (!File.Exists(_backupKeyFilePath))
                return Task.FromResult((false, "백업 키 파일을 찾을 수 없습니다."));

            try
            {
                _logger.LogInformation("백업 키로부터 마스터 키 복구를 시도합니다.");

                // 1. 백업 키 복호화
                _masterKey = DecryptBackupKey(_backupKeyFilePath, recoveryPassword);

                // 2. DPAPI로 보호하여 저장
                SaveWithDPAPI(_masterKey);

                _logger.LogInformation("마스터 키를 성공적으로 복구했습니다.");
                WriteEventLog("백업 키로부터 마스터 키를 성공적으로 복구했습니다.", EventLogEntryType.Information);

                return Task.FromResult((true, "마스터 키를 성공적으로 복구했습니다."));
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("복구 암호가 올바르지 않습니다.");
                return Task.FromResult((false, "복구 암호가 올바르지 않습니다. 다시 시도하세요."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "마스터 키 복구 중 오류 발생");
                WriteEventLog($"마스터 키 복구 실패: {ex.Message}", EventLogEntryType.Error);
                return Task.FromResult((false, $"복구 실패: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// 백업 키 파일 다운로드를 위한 경로 반환
    /// </summary>
    public string GetBackupKeyFilePath()
    {
        return _backupKeyFilePath;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 기존 마스터 키 파일이 있으면 자동으로 로드 (콘솔 입력 없음)
    /// </summary>
    private void TryAutoLoad()
    {
        if (File.Exists(_keyFilePath))
        {
            try
            {
                var encryptedKey = File.ReadAllBytes(_keyFilePath);
                _masterKey = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.LocalMachine);
                _logger.LogInformation("기존 마스터 키를 성공적으로 로드했습니다.");
                WriteEventLog("마스터 키를 성공적으로 로드했습니다.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "마스터 키 자동 로드 실패. Setup 페이지에서 복구가 필요합니다.");
                WriteEventLog($"마스터 키 자동 로드 실패: {ex.Message}", EventLogEntryType.Warning);
                // 로드 실패는 무시 (WEB UI에서 처리)
            }
        }
    }

    /// <summary>
    /// 새로운 마스터 키 생성 (32바이트 = 256비트)
    /// </summary>
    private byte[] GenerateNewMasterKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// DPAPI로 보호된 키를 저장
    /// </summary>
    private void SaveWithDPAPI(byte[] key)
    {
        var encryptedKey = ProtectedData.Protect(key, null, DataProtectionScope.LocalMachine);
        Directory.CreateDirectory(Path.GetDirectoryName(_keyFilePath)!);
        File.WriteAllBytes(_keyFilePath, encryptedKey);
    }

    /// <summary>
    /// 백업 키를 저장 (PBKDF2 + AES-256-GCM)
    /// </summary>
    private void SaveBackupKey(byte[] key, string password)
    {
        // PBKDF2로 암호에서 키 생성 (100,000번 반복)
        using var pbkdf2 = new Rfc2898DeriveBytes(password, 32, 100000, HashAlgorithmName.SHA256);
        var keyEncryptionKey = pbkdf2.GetBytes(32);
        var salt = pbkdf2.Salt;

        // AES-256-GCM으로 암호화
        var tagSize = AesGcm.TagByteSizes.MaxSize;
        using var aes = new AesGcm(keyEncryptionKey, tagSize);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherText = new byte[key.Length];
        var tag = new byte[tagSize];

        aes.Encrypt(nonce, key, cipherText, tag);

        // 백업 파일 형식: [salt(32)][nonce(12)][tag(16)][cipherText(32)]
        Directory.CreateDirectory(Path.GetDirectoryName(_backupKeyFilePath)!);
        using var fs = File.Create(_backupKeyFilePath);
        fs.Write(salt);
        fs.Write(nonce);
        fs.Write(tag);
        fs.Write(cipherText);
        fs.Close();

        Array.Clear(keyEncryptionKey, 0, keyEncryptionKey.Length);
    }

    /// <summary>
    /// 백업 키를 복호화하여 마스터 키 반환
    /// </summary>
    private byte[] DecryptBackupKey(string filePath, string password)
    {
        using var fs = File.OpenRead(filePath);

        // 파일에서 각 부분 읽기
        var salt = new byte[32];
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipherText = new byte[fs.Length - 32 - 12 - 16];

        fs.Read(salt);
        fs.Read(nonce);
        fs.Read(tag);
        fs.Read(cipherText);
        fs.Close();

        // 암호에서 키 생성
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations: 100000, HashAlgorithmName.SHA256);
        var keyEncryptionKey = pbkdf2.GetBytes(32);

        try
        {
            // 복호화
            var tagSize = tag.Length;
            using var aes = new AesGcm(keyEncryptionKey, tagSize);
            var plainText = new byte[cipherText.Length];
            aes.Decrypt(nonce, cipherText, tag, plainText);
            return plainText;
        }
        finally
        {
            Array.Clear(keyEncryptionKey, 0, keyEncryptionKey.Length);
        }
    }

    /// <summary>
    /// Windows 이벤트 로그에 기록
    /// </summary>
    private void WriteEventLog(string message, EventLogEntryType type)
    {
        try
        {
            if (!EventLog.SourceExists(Consts.SECUiDEA_KMS))
            {
                EventLog.WriteEntry(Consts.Application, $"[{Consts.SECUiDEA_KMS}] {message}", type);
            }
            else
            {
                EventLog.WriteEntry(Consts.SECUiDEA_KMS, message, type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "이벤트 로그 쓰기 실패");
        }
    }

    #endregion
}
