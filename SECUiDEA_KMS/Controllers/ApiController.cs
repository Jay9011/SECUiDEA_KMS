using Microsoft.AspNetCore.Mvc;
using SECUiDEA_KMS.Models;
using SECUiDEA_KMS.Models.EncryptionKeys;
using SECUiDEA_KMS.Models.KeyRequests;
using SECUiDEA_KMS.Services;

namespace SECUiDEA_KMS.Controllers;

/// <summary>
/// 외부 클라이언트용 API 컨트롤러
/// 키 생성 및 조회 API 제공
/// </summary>
[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly KeyService _keyService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(KeyService keyService, ILogger<ApiController> logger)
    {
        _keyService = keyService;
        _logger = logger;
    }

    /// <summary>
    /// 상태 체크 API
    /// GET /api/health
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// GET /api/health
    /// </remarks>
    [ProducesResponseType(typeof(KmsResponse), 200)]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return MapKmsResponse(new KmsResponse { ErrorCode = "0000", ErrorMessage = "Success" });
    }

    /// <summary>
    /// 외부 클라이언트가 새 암호화 키 생성 요청
    /// POST /api/keys/generate
    /// </summary>
    /// <remarks>
    /// 헤더에 X-Client-Guid를 포함해야 합니다.
    /// 
    /// 요청 예시:
    /// POST /api/keys/generate
    /// Headers:
    ///   X-Client-Guid: {guid}
    /// Body:
    /// {
    ///   "isAutoRotation": false,
    ///   "expirationDays": null,
    ///   "rotationScheduleDays": null
    /// }
    /// </remarks>
    [HttpPost("keys/generate")]
    [ProducesResponseType(typeof(KmsResponse<EncryptionKeyEntity>), 200)]
    [ProducesResponseType(typeof(KmsResponse), 400)]
    [ProducesResponseType(typeof(KmsResponse), 403)]
    [ProducesResponseType(typeof(KmsResponse), 404)]
    [ProducesResponseType(typeof(KmsResponse), 409)]
    [ProducesResponseType(typeof(KmsResponse), 429)]
    public async Task<IActionResult> GenerateKey([FromBody] KeyGenerationReqDTO request)
    {
        // 헤더에서 ClientGuid 추출
        if (!Request.Headers.TryGetValue("X-Client-Guid", out var guidHeader) ||
            !Guid.TryParse(guidHeader.FirstOrDefault(), out var clientGuid))
        {
            _logger.LogWarning("키 생성 요청에 X-Client-Guid 헤더가 없거나 유효하지 않습니다.");
            return BadRequest(new KmsResponse
            {
                ErrorCode = "9999",
                ErrorMessage = "X-Client-Guid 헤더가 필요합니다."
            });
        }

        request.ClientGuid = clientGuid;

        if (!ModelState.IsValid)
        {
            return BadRequest(new KmsResponse
            {
                ErrorCode = "9999",
                ErrorMessage = "입력 값이 유효하지 않습니다."
            });
        }

        // 자동 회전 검증
        if (request.IsAutoRotation)
        {
            if (!request.ExpirationDays.HasValue || request.ExpirationDays.Value <= 0)
            {
                return BadRequest(new KmsResponse
                {
                    ErrorCode = "2002",
                    ErrorMessage = "ExpirationDays is required when IsAutoRotation is true"
                });
            }
            if (!request.RotationScheduleDays.HasValue || request.RotationScheduleDays.Value <= 0)
            {
                return BadRequest(new KmsResponse
                {
                    ErrorCode = "2003",
                    ErrorMessage = "RotationScheduleDays is required when IsAutoRotation is true"
                });
            }
        }

        // 외부 클라이언트 요청이므로 IP 검증 수행
        var response = await _keyService.GenerateKeyAsync(request, skipIpValidation: false);

        return MapKmsResponse(response);
    }

    /// <summary>
    /// 외부 클라이언트가 활성 키 조회 요청
    /// GET /api/keys
    /// </summary>
    /// <remarks>
    /// 헤더에 X-Client-Guid를 포함해야 합니다.
    /// 
    /// 요청 예시:
    /// GET /api/keys
    /// Headers:
    ///   X-Client-Guid: {guid}
    /// </remarks>
    [HttpGet("keys")]
    [ProducesResponseType(typeof(KmsResponse<string>), 200)]
    [ProducesResponseType(typeof(KmsResponse), 400)]
    [ProducesResponseType(typeof(KmsResponse), 403)]
    [ProducesResponseType(typeof(KmsResponse), 404)]
    [ProducesResponseType(typeof(KmsResponse), 429)]
    public async Task<IActionResult> GetKey()
    {
        // 헤더에서 ClientGuid 추출
        if (!Request.Headers.TryGetValue("X-Client-Guid", out var guidHeader) ||
            !Guid.TryParse(guidHeader.FirstOrDefault(), out var clientGuid))
        {
            _logger.LogWarning("키 조회 요청에 X-Client-Guid 헤더가 없거나 유효하지 않습니다.");
            return BadRequest(new KmsResponse
            {
                ErrorCode = "9999",
                ErrorMessage = "X-Client-Guid 헤더가 필요합니다."
            });
        }

        var response = await _keyService.GetKeyAsync(clientGuid);

        return MapKmsResponse(response);
    }

    /// <summary>
    /// 이전 버전의 Key 획득 요청
    /// GET /api/keys/previous
    /// </summary>
    /// <remarks>
    /// 헤더에 X-Client-Guid를 포함해야 합니다.
    /// 
    /// 요청 예시:
    /// GET /api/keys/previous
    /// Headers:
    ///   X-Client-Guid: {guid}

    [HttpGet("keys/previous")]
    [ProducesResponseType(typeof(KmsResponse<string>), 200)]
    [ProducesResponseType(typeof(KmsResponse), 400)]
    [ProducesResponseType(typeof(KmsResponse), 403)]
    [ProducesResponseType(typeof(KmsResponse), 404)]
    [ProducesResponseType(typeof(KmsResponse), 429)]
    public async Task<IActionResult> GetPreviousKey()
    {
        // 헤더에서 ClientGuid 추출
        if (!Request.Headers.TryGetValue("X-Client-Guid", out var guidHeader) ||
            !Guid.TryParse(guidHeader.FirstOrDefault(), out var clientGuid))
        {
            _logger.LogWarning("이전 버전의 Key 획득 요청에 X-Client-Guid 헤더가 없거나 유효하지 않습니다.");
            return BadRequest(new KmsResponse
            {
                ErrorCode = "9999",
                ErrorMessage = "X-Client-Guid 헤더가 필요합니다."
            });
        }

        var response = await _keyService.GetPreviousKeyAsync(clientGuid);

        return MapKmsResponse(response);
    }

    /// <summary>
    /// KMS ErrorCode를 HTTP 상태 코드로 매핑
    /// </summary>
    private IActionResult MapKmsResponse<T>(KmsResponse<T> response)
    {
        return response.ErrorCode switch
        {
            "0000" => Ok(response),
            "1001" => NotFound(response), // Client Not Found or Inactive
            "1002" => StatusCode(403, response), // IP Address Not Allowed
            "1003" => Conflict(response), // ClientIP Already Registered
            "2001" => NotFound(response), // No Active Key Found
            "2002" => BadRequest(response), // ExpirationDays Required
            "2003" => BadRequest(response), // RotationScheduleDays Required
            "9999" => response.ErrorMessage.Contains("Rate limit", StringComparison.OrdinalIgnoreCase)
                ? StatusCode(429, response) // Rate Limit Exceeded
                : BadRequest(response), // General Error
            _ => StatusCode(500, response) // Unknown Error
        };
    }
}

