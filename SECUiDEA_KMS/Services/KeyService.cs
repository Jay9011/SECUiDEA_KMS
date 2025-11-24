using CryptoManager;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.ClientServers;
using SECUiDEA_KMS.Models.EncryptionKeys;
using SECUiDEA_KMS.Models.KeyRequests;
using SECUiDEA_KMS.Repositories;
using System.Security.Cryptography;

namespace SECUiDEA_KMS.Services;

/// <summary>
/// 키 관리 서비스
/// 키 생성/조회 및 암호화/복호화 처리
/// </summary>
public class KeyService
{
    #region 의존 주입
    private readonly IKeyRepository _keyRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICryptoManager _cryptoManager;
    private readonly HttpContextHelper _httpContextHelper;
    private readonly ILogger<KeyService> _logger;

    public KeyService(
        IKeyRepository keyRepository,
        IClientRepository clientRepository,
        ICryptoManager cryptoManager,
        HttpContextHelper httpContextHelper,
        ILogger<KeyService> logger)
    {
        _keyRepository = keyRepository;
        _clientRepository = clientRepository;
        _cryptoManager = cryptoManager;
        _httpContextHelper = httpContextHelper;
        _logger = logger;
    }
    #endregion

    /// <summary>
    /// 새 암호화 키 생성
    /// 클라이언트용 AES-256 키를 생성하고 마스터 키로 암호화하여 저장
    /// </summary>
    /// <param name="request">키 생성 요청 정보</param>
    /// <param name="skipIpValidation">IP 검증 스킵 여부 (관리자용 = true, 외부 클라이언트 = false)</param>
    public async Task<KmsResponse<EncryptionKeyEntity>> GenerateKeyAsync(KeyGenerationReqDTO request, bool skipIpValidation = false)
    {
        try
        {
            _logger.LogInformation("키 생성 시작: ClientGuid={ClientGuid}, IsAutoRotation={IsAutoRotation}, SkipIpValidation={SkipIpValidation}",
                request.ClientGuid, request.IsAutoRotation, skipIpValidation);

            // 요청 정보 추출
            var requestInfo = _httpContextHelper.GetRequestInfo();

            // IP 검증 (외부 클라이언트만)
            if (!skipIpValidation)
            {
                var clientInfo = await _clientRepository.GetClientInfoAsync(new ClientServerEntity { ClientGuid = request.ClientGuid });

                if (!clientInfo.IsSuccess || clientInfo.Data == null)
                {
                    _logger.LogWarning("클라이언트 정보 조회 실패: ClientGuid={ClientGuid}", request.ClientGuid);
                    return new KmsResponse<EncryptionKeyEntity>
                    {
                        ErrorCode = "1001",
                        ErrorMessage = "Client not found or inactive",
                        Data = null
                    };
                }

                // Strict 모드에서 IP 검증
                if (clientInfo.Data.IPValidationMode == "Strict")
                {
                    if (requestInfo.RequestIP != clientInfo.Data.ClientIP)
                    {
                        _logger.LogWarning("IP 불일치: ClientGuid={ClientGuid}, 등록IP={RegisteredIP}, 요청IP={RequestIP}",
                            request.ClientGuid, clientInfo.Data.ClientIP, requestInfo.RequestIP);

                        return new KmsResponse<EncryptionKeyEntity>
                        {
                            ErrorCode = "1002",
                            ErrorMessage = "IP address not allowed",
                            Data = null
                        };
                    }
                }

                _logger.LogInformation("IP 검증 통과: ClientGuid={ClientGuid}, IP={RequestIP}, Mode={Mode}",
                    request.ClientGuid, requestInfo.RequestIP, clientInfo.Data.IPValidationMode);
            }
            else
            {
                _logger.LogInformation("IP 검증 스킵 (관리자 요청): ClientGuid={ClientGuid}", request.ClientGuid);
            }

            // 새 AES-256 키 생성 (32바이트 = 256비트)
            var aesKey = new byte[32];
            RandomNumberGenerator.Fill(aesKey);

            // 마스터 키로 암호화
            var encryptedKeyData = Convert.FromBase64String(_cryptoManager.Encrypt(Convert.ToBase64String(aesKey)));

            // Repository를 통해 DB에 저장
            var response = await _keyRepository.GenerateKeyAsync(
                new GenerateKey_Proc()
                {
                    ClientGuid = request.ClientGuid,
                    EncryptedKeyData = encryptedKeyData,
                    IsAutoRotation = request.IsAutoRotation,
                    ExpirationDays = request.ExpirationDays,
                    RotationScheduleDays = request.RotationScheduleDays,
                    SkipIpValidation = skipIpValidation  // SP에 플래그 전달
                },
                requestInfo);

            if (response.IsSuccess)
            {
                _logger.LogInformation("키 생성 성공: KeyId={KeyId}", response.Data?.KeyId);
            }
            else
            {
                _logger.LogWarning("키 생성 실패: {ErrorCode} - {ErrorMessage}", response.ErrorCode, response.ErrorMessage);
            }

            // 메모리에서 평문 키 제거
            Array.Clear(aesKey, 0, aesKey.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "키 생성 중 예외 발생");
            return new KmsResponse<EncryptionKeyEntity>
            {
                ErrorCode = "9999",
                ErrorMessage = $"Service Exception: 키 생성 중 오류 발생: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 활성 키 조회
    /// DB에서 암호화된 키를 조회하고 복호화하여 반환
    /// </summary>
    public async Task<KmsResponse<string>> GetKeyAsync(Guid clientGuid)
    {
        return await GetAndDecryptKeyAsync(clientGuid, "Current", "키 조회");
    }

    /// <summary>
    /// 이전 버전의 Key 획득 요청
    /// </summary>
    /// <param name="clientGuid">클라이언트 GUID</param>
    /// <returns>이전 버전의 Key</returns>
    public async Task<KmsResponse<string>> GetPreviousKeyAsync(Guid clientGuid)
    {
        return await GetAndDecryptKeyAsync(clientGuid, "Previous", "이전 버전의 Key 획득");
    }

    /// <summary>
    /// 키 정보 조회 (암호화된 상태 그대로, 관리자용)
    /// </summary>
    public async Task<KmsResponse<EncryptionKeyEntity>> GetKeyInfoAsync(Guid clientGuid)
    {
        try
        {
            _logger.LogInformation("키 정보 조회 시작: ClientGuid={ClientGuid}", clientGuid);

            var requestInfo = _httpContextHelper.GetRequestInfo();
            var response = await _keyRepository.GetKeyAsync(new GetKey_Proc()
            {
                ClientGuid = clientGuid
            }, requestInfo);

            if (response.IsSuccess)
            {
                _logger.LogInformation("키 정보 조회 성공: KeyId={KeyId}", response.Data?.KeyId);
            }
            else
            {
                _logger.LogWarning("키 정보 조회 실패: {ErrorCode} - {ErrorMessage}", response.ErrorCode, response.ErrorMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "키 정보 조회 중 예외 발생");
            return CreateErrorResponse<EncryptionKeyEntity>("9999", $"Service Exception: 키 정보 조회 중 오류 발생: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// 암호화된 키 데이터를 복호화하여 Base64 문자열로 반환
    /// </summary>
    private string DecryptKeyData(byte[] encryptedKeyData)
    {
        var encryptedKeyBase64 = Convert.ToBase64String(encryptedKeyData);
        return _cryptoManager.Decrypt(encryptedKeyBase64);
    }

    /// <summary>
    /// 에러 응답 생성 헬퍼 메서드
    /// </summary>
    private KmsResponse<T> CreateErrorResponse<T>(string errorCode, string errorMessage)
    {
        return new KmsResponse<T>
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Data = default
        };
    }

    /// <summary>
    /// 키 조회 및 복호화 공통 로직
    /// </summary>
    private async Task<KmsResponse<string>> GetAndDecryptKeyAsync(
        Guid clientGuid,
        string keyType = "Current",
        string operationName = "키 조회")
    {
        try
        {
            _logger.LogInformation("{OperationName} 시작: ClientGuid={ClientGuid}", operationName, clientGuid);

            var requestInfo = _httpContextHelper.GetRequestInfo();

            var proc = new GetKey_Proc
            {
                ClientGuid = clientGuid,
                Type = keyType != "Current" ? keyType : null
            };

            var response = keyType == "Previous"
                ? await _keyRepository.GetPreviousKeyAsync(proc, requestInfo)
                : await _keyRepository.GetKeyAsync(proc, requestInfo);

            if (!response.IsSuccess || response.Data == null)
            {
                _logger.LogWarning("{OperationName} 실패: {ErrorCode} - {ErrorMessage}",
                    operationName, response.ErrorCode, response.ErrorMessage);
                return CreateErrorResponse<string>(response.ErrorCode, response.ErrorMessage);
            }

            // 마스터 키로 복호화
            var decryptedKeyBase64 = DecryptKeyData(response.Data.EncryptedKeyData);

            _logger.LogInformation("{OperationName} 성공: KeyId={KeyId}, KeyVersion={KeyVersion}",
                operationName, response.Data.KeyId, response.Data.KeyVersion);

            return new KmsResponse<string>
            {
                ErrorCode = "0000",
                ErrorMessage = "Success",
                Data = decryptedKeyBase64
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{OperationName} 중 예외 발생", operationName);
            return CreateErrorResponse<string>("9999", $"Service Exception: {operationName} 중 오류 발생: {ex.Message}");
        }
    }

    #endregion
}


